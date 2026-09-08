using Loren.Infrastructure.CanonicalState;
using Loren.Infrastructure.CanonicalState.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Loren.IntegrationTests;

public sealed class CanonicalStateMigrationDriftTests
{
    [Fact]
    public async Task LatestProposalMigrationIsPresent()
    {
        DbContextOptions<CanonicalStateDbContext> options = new DbContextOptionsBuilder<CanonicalStateDbContext>()
            .UseSqlite($"Data Source=file:drift-{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;
        await using CanonicalStateDbContext context = new(options);
        await CanonicalStateDatabase.MigrateAsync(context, TestContext.Current.CancellationToken);
        Assert.Contains("202609070002_AddCreateBranchProposals", await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
    }

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
        IModelRuntimeInitializer runtimeInitializer = context.GetService<IModelRuntimeInitializer>();
        IModel snapshotModel = runtimeInitializer.Initialize(
            new CanonicalStateDbContextModelSnapshot().Model,
            designTime: true);

        IReadOnlyList<MigrationOperation> differences = differ.GetDifferences(
            snapshotModel.GetRelationalModel(),
            designTimeModel.Model.GetRelationalModel());

        Assert.True(
            differences.Count == 0,
            "Pending canonical-state model changes: "
            + string.Join(", ", differences.Select(Describe)));
    }

    private static string Describe(MigrationOperation operation) => operation switch
    {
        AddColumnOperation add => $"AddColumn {add.Table}.{add.Name} ({add.ClrType.Name})",
        DropColumnOperation drop => $"DropColumn {drop.Table}.{drop.Name}",
        CreateIndexOperation index => $"CreateIndex {index.Table}.{index.Name}",
        DropIndexOperation index => $"DropIndex {index.Table}.{index.Name}",
        AddForeignKeyOperation foreignKey =>
            $"AddForeignKey {foreignKey.Table}.{foreignKey.Name}",
        DropForeignKeyOperation foreignKey =>
            $"DropForeignKey {foreignKey.Table}.{foreignKey.Name}",
        _ => operation.GetType().Name,
    };
}
