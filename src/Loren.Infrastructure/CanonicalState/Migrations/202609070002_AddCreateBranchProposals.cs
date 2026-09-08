using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Loren.Infrastructure.CanonicalState.Migrations;

[DbContext(typeof(CanonicalStateDbContext))]
[Migration("202609070002_AddCreateBranchProposals")]
public sealed class AddCreateBranchProposals : Migration
{
    private static readonly string[] OwnerStatusExpiryColumns = ["OwnerPrincipalReference", "Status", "ExpiresAtUnixMs"];
    private static readonly string[] ProjectRepositoryColumns = ["ProjectId", "RepositoryId"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CreateBranchProposals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                OwnerPrincipalReference = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                RepositoryProvider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                RepositoryNamespace = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                RepositoryName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Branch = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                SourceRef = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                SourceSha = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                IntentFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                CreatedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
                ExpiresAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
                DecidedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CreateBranchProposals", p => p.Id);
                table.ForeignKey("FK_CreateBranchProposals_Projects_ProjectId", p => p.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_CreateBranchProposals_Repositories_RepositoryId", p => p.RepositoryId, "Repositories", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_CreateBranchProposals_OwnerPrincipalReference_Status_ExpiresAtUnixMs", "CreateBranchProposals", OwnerStatusExpiryColumns);
        migrationBuilder.CreateIndex("IX_CreateBranchProposals_ProjectId_RepositoryId", "CreateBranchProposals", ProjectRepositoryColumns);
        migrationBuilder.CreateIndex("IX_CreateBranchProposals_RepositoryId", "CreateBranchProposals", "RepositoryId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "CreateBranchProposals");
}
