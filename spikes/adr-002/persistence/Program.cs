using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

string root = Path.Combine(Path.GetTempPath(), "loren-m0-persistence", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
string databasePath = Path.Combine(root, "loren.db");
string connectionString = $"Data Source={databasePath}";

var owner = new OwnerRecord(Guid.NewGuid(), "owner");
var project = new ProjectRecord(Guid.NewGuid(), "Loren", "loren", "rua-den/loren");
var audit = new AuditRecord(Guid.NewGuid(), "m0.seed", DateTimeOffset.UtcNow);

await using (var db = LorenSpikeDbContext.Create(connectionString))
{
    await db.Database.MigrateAsync();
    db.Owners.Add(owner);
    db.Projects.Add(project);
    db.AuditEvents.Add(audit);
    await db.SaveChangesAsync();
}

LorenExport export;
await using (var db = LorenSpikeDbContext.Create(connectionString))
{
    export = new LorenExport(
        await db.Owners.AsNoTracking().ToArrayAsync(),
        await db.Projects.AsNoTracking().ToArrayAsync(),
        await db.AuditEvents.AsNoTracking().ToArrayAsync());
}

string exportPath = Path.Combine(root, "export.json");
await File.WriteAllTextAsync(exportPath, JsonSerializer.Serialize(export));
File.Delete(databasePath);

LorenExport restored = JsonSerializer.Deserialize<LorenExport>(await File.ReadAllTextAsync(exportPath))
    ?? throw new InvalidOperationException("Could not deserialize Loren export.");

await using (var db = LorenSpikeDbContext.Create(connectionString))
{
    await db.Database.MigrateAsync();
    db.Owners.AddRange(restored.Owners);
    db.Projects.AddRange(restored.Projects);
    db.AuditEvents.AddRange(restored.AuditEvents);
    await db.SaveChangesAsync();
}

await using (var db = LorenSpikeDbContext.Create(connectionString))
{
    ProjectRecord? restoredProject = await db.Projects.SingleOrDefaultAsync(x => x.Alias == "loren");
    int auditCount = await db.AuditEvents.CountAsync();

    if (restoredProject?.Repository != "rua-den/loren" || auditCount != 1)
    {
        throw new InvalidOperationException("Restored canonical state did not match the exported state.");
    }
}

Console.WriteLine("[spike] PASS: EF migration -> persist -> export -> wipe -> migrate -> restore -> reload");

internal sealed class LorenSpikeDbContext(DbContextOptions<LorenSpikeDbContext> options) : DbContext(options)
{
    public DbSet<OwnerRecord> Owners => Set<OwnerRecord>();
    public DbSet<ProjectRecord> Projects => Set<ProjectRecord>();
    public DbSet<AuditRecord> AuditEvents => Set<AuditRecord>();

    public static LorenSpikeDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<LorenSpikeDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new LorenSpikeDbContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OwnerRecord>().HasKey(x => x.Id);
        modelBuilder.Entity<ProjectRecord>().HasKey(x => x.Id);
        modelBuilder.Entity<ProjectRecord>().HasIndex(x => x.Alias).IsUnique();
        modelBuilder.Entity<AuditRecord>().HasKey(x => x.Id);
    }
}

internal sealed class LorenSpikeDesignTimeFactory : IDesignTimeDbContextFactory<LorenSpikeDbContext>
{
    public LorenSpikeDbContext CreateDbContext(string[] args) =>
        LorenSpikeDbContext.Create("Data Source=loren-m0-design.db");
}

internal sealed record OwnerRecord(Guid Id, string Name);
internal sealed record ProjectRecord(Guid Id, string Name, string Alias, string Repository);
internal sealed record AuditRecord(Guid Id, string EventType, DateTimeOffset OccurredAt);
internal sealed record LorenExport(
    IReadOnlyList<OwnerRecord> Owners,
    IReadOnlyList<ProjectRecord> Projects,
    IReadOnlyList<AuditRecord> AuditEvents);
