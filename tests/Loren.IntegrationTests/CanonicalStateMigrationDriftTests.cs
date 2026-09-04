using Loren.Infrastructure.CanonicalState;
using Loren.Infrastructure.CanonicalState.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class CanonicalStateMigrationDriftTests
{
    [Fact]
    public void SnapshotMatchesCurrentCanonicalStateModel()
    {
        DbContextOptions<CanonicalStateDbContext> options =
            new DbContextOptionsBuilder<CanonicalStateDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

        using CanonicalStateDbContext context = new(options);
        IMigrationsModelDiffer differ = context.GetService<IMigrationsModelDiffer>();
        IDesignTimeModel designTimeModel = context.GetService<IDesignTimeModel>();
        IModel snapshotModel = new CanonicalStateDbContextModelSnapshot().Model;

        IReadOnlyList<MigrationOperation> differences = differ.GetDifferences(
            snapshotModel.GetRelationalModel(),
            designTimeModel.Model.GetRelationalModel());

        Assert.True(
            differences.Count == 0,
            "Pending canonical-state model changes: "
            + string.Join(", ", differences.Select(operation => operation.GetType().Name)));
    }
}
