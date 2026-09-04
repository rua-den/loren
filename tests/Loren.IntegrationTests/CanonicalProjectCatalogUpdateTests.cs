using Loren.Core.Projects;
using Loren.Infrastructure.CanonicalState;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class CanonicalProjectCatalogUpdateTests
{
    [Fact]
    public async Task SameCatalogCanReplaceAliasesForExistingProject()
    {
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"loren-m3-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        string databasePath = Path.Combine(tempDirectory, "loren.db");
        string connectionString = $"Data Source={databasePath}";
        DateTimeOffset createdAt = new(2026, 9, 4, 5, 30, 0, TimeSpan.Zero);
        DateTimeOffset updatedAt = createdAt.AddMinutes(5);
        ProjectId projectId = ProjectId.New();

        try
        {
            DbContextOptions<CanonicalStateDbContext> options =
                new DbContextOptionsBuilder<CanonicalStateDbContext>()
                    .UseSqlite(connectionString)
                    .Options;

            await using CanonicalStateDbContext context = new(options);
            await CanonicalStateDatabase.MigrateAsync(context);
            SqliteProjectCatalog catalog = new(context);

            await catalog.SaveAsync(
                new ProjectSnapshot(
                    new Project(
                        projectId,
                        "Wedding Online",
                        ["old alias", "wedding-online"],
                        createdAt,
                        createdAt),
                    []));

            await catalog.SaveAsync(
                new ProjectSnapshot(
                    new Project(
                        projectId,
                        "Wedding Online",
                        ["new alias", "wedding-online"],
                        createdAt,
                        updatedAt),
                    []));

            Assert.Null(await catalog.FindByAliasAsync("old alias"));

            ProjectSnapshot? updated = await catalog.FindByAliasAsync("new alias");
            Assert.NotNull(updated);
            Assert.Equal(projectId, updated.Project.Id);
            Assert.Equal(updatedAt, updated.Project.UpdatedAt);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
