using Loren.Core.Projects;

namespace Loren.Core.Memories;

public interface IMemoryStore
{
    Task AddAsync(
        MemoryRecord record,
        CancellationToken cancellationToken = default);

    Task CorrectAsync(
        MemoryRecordId currentMemoryRecordId,
        MemoryRecord correction,
        CancellationToken cancellationToken = default);

    Task ForgetAsync(
        MemoryRecordId currentMemoryRecordId,
        CancellationToken cancellationToken = default);

    Task<MemoryRecord?> GetAsync(
        MemoryRecordId memoryRecordId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryRecord>> ListCurrentForProjectAsync(
        ProjectId projectId,
        CancellationToken cancellationToken = default);
}
