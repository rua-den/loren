using Loren.Core.Memories;
using Loren.Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Loren.Infrastructure.CanonicalState;

public sealed class SqliteMemoryStore : IMemoryStore
{
    private const string OwnerExplicit = "OWNER_EXPLICIT";
    private const string OwnerCorrection = "OWNER_CORRECTION";
    private const string VerifiedTool = "VERIFIED_TOOL";
    private const string OwnerApprovedInference = "OWNER_APPROVED_INFERENCE";
    private const string ModelInference = "MODEL_INFERENCE";
    private const string ExternalContent = "EXTERNAL_CONTENT";

    private readonly CanonicalStateDbContext _dbContext;

    public SqliteMemoryStore(CanonicalStateDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task AddAsync(
        MemoryRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await ValidateScopeAsync(record, cancellationToken);

        _dbContext.MemoryRecords.Add(ToRow(record));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CorrectAsync(
        MemoryRecordId currentMemoryRecordId,
        MemoryRecord correction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(correction);

        if (correction.SourceClass != MemorySourceClass.OwnerCorrection)
        {
            throw new InvalidOperationException(
                "Memory correction must use OWNER_CORRECTION source authority.");
        }

        if (correction.SupersededById is not null)
        {
            throw new InvalidOperationException(
                "A newly appended correction must be current when it is created.");
        }

        if (correction.Id == currentMemoryRecordId)
        {
            throw new InvalidOperationException(
                "A correction must use a new MemoryRecordId.");
        }

        await ValidateScopeAsync(correction, cancellationToken);

        _dbContext.ChangeTracker.Clear();
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            MemoryRecordRow current = await _dbContext.MemoryRecords
                .SingleOrDefaultAsync(
                    memory => memory.Id == currentMemoryRecordId.Value,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Memory record '{currentMemoryRecordId}' does not exist.");

            if (current.SupersededById is not null)
            {
                throw new InvalidOperationException(
                    $"Memory record '{currentMemoryRecordId}' is already superseded.");
            }

            if (current.ProjectId != correction.ProjectId?.Value
                || current.RepositoryId != correction.RepositoryId?.Value)
            {
                throw new InvalidOperationException(
                    "A correction must keep the same Project/Repository scope as the current memory.");
            }

            if (correction.CreatedAt < current.CreatedAt)
            {
                throw new InvalidOperationException(
                    "A correction cannot be created before the memory it supersedes.");
            }

            bool correctionIdAlreadyExists = await _dbContext.MemoryRecords
                .AsNoTracking()
                .AnyAsync(memory => memory.Id == correction.Id.Value, cancellationToken);

            if (correctionIdAlreadyExists)
            {
                throw new InvalidOperationException(
                    $"Memory record '{correction.Id}' already exists.");
            }

            _dbContext.MemoryRecords.Add(ToRow(correction));
            await _dbContext.SaveChangesAsync(cancellationToken);

            current.SupersededById = correction.Id.Value;
            current.UpdatedAt = correction.CreatedAt;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    public async Task<MemoryRecord?> GetAsync(
        MemoryRecordId memoryRecordId,
        CancellationToken cancellationToken = default)
    {
        if (memoryRecordId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Memory record ID cannot be empty.",
                nameof(memoryRecordId));
        }

        MemoryRecordRow? row = await _dbContext.MemoryRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                memory => memory.Id == memoryRecordId.Value,
                cancellationToken);

        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<MemoryRecord>> ListCurrentForProjectAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default)
    {
        if (projectId.Value == Guid.Empty)
        {
            throw new ArgumentException("Project ID cannot be empty.", nameof(projectId));
        }

        MemoryRecordRow[] rows = await _dbContext.MemoryRecords
            .AsNoTracking()
            .Where(memory =>
                memory.ProjectId == projectId.Value
                && memory.SupersededById == null)
            .ToArrayAsync(cancellationToken);

        return rows
            .OrderBy(memory => memory.CreatedAt)
            .ThenBy(memory => memory.Id)
            .Select(Map)
            .ToArray();
    }

    private async Task ValidateScopeAsync(
        MemoryRecord record,
        CancellationToken cancellationToken)
    {
        if (record.RepositoryId is not RepositoryId repositoryId)
        {
            return;
        }

        ProjectId projectId = record.ProjectId
            ?? throw new InvalidOperationException(
                "Repository-scoped memory must also include its Project ID.");

        Guid? repositoryProjectId = await _dbContext.Repositories
            .AsNoTracking()
            .Where(repository => repository.Id == repositoryId.Value)
            .Select(repository => (Guid?)repository.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);

        if (repositoryProjectId is null)
        {
            throw new InvalidOperationException(
                $"Repository '{repositoryId}' does not exist in canonical state.");
        }

        if (repositoryProjectId.Value != projectId.Value)
        {
            throw new InvalidOperationException(
                $"Repository '{repositoryId}' does not belong to Project '{projectId}'.");
        }
    }

    private static MemoryRecordRow ToRow(MemoryRecord record) => new()
    {
        Id = record.Id.Value,
        SourceClass = ToStorageValue(record.SourceClass),
        Content = record.Content,
        ProjectId = record.ProjectId?.Value,
        RepositoryId = record.RepositoryId?.Value,
        SourceReference = record.SourceReference,
        SupersededById = record.SupersededById?.Value,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
    };

    private static MemoryRecord Map(MemoryRecordRow row) => new(
        new MemoryRecordId(row.Id),
        FromStorageValue(row.SourceClass),
        row.Content,
        row.ProjectId is Guid projectId ? new ProjectId(projectId) : null,
        row.RepositoryId is Guid repositoryId ? new RepositoryId(repositoryId) : null,
        row.SourceReference,
        row.SupersededById is Guid supersededById
            ? new MemoryRecordId(supersededById)
            : null,
        row.CreatedAt,
        row.UpdatedAt);

    private static string ToStorageValue(MemorySourceClass sourceClass) => sourceClass switch
    {
        MemorySourceClass.OwnerExplicit => OwnerExplicit,
        MemorySourceClass.OwnerCorrection => OwnerCorrection,
        MemorySourceClass.VerifiedTool => VerifiedTool,
        MemorySourceClass.OwnerApprovedInference => OwnerApprovedInference,
        MemorySourceClass.ModelInference => ModelInference,
        MemorySourceClass.ExternalContent => ExternalContent,
        _ => throw new ArgumentOutOfRangeException(nameof(sourceClass)),
    };

    private static MemorySourceClass FromStorageValue(string sourceClass) => sourceClass switch
    {
        OwnerExplicit => MemorySourceClass.OwnerExplicit,
        OwnerCorrection => MemorySourceClass.OwnerCorrection,
        VerifiedTool => MemorySourceClass.VerifiedTool,
        OwnerApprovedInference => MemorySourceClass.OwnerApprovedInference,
        ModelInference => MemorySourceClass.ModelInference,
        ExternalContent => MemorySourceClass.ExternalContent,
        _ => throw new InvalidOperationException(
            $"Unknown durable memory source class '{sourceClass}'."),
    };
}
