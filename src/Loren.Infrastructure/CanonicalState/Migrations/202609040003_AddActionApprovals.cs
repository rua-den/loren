using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Loren.Infrastructure.CanonicalState.Migrations;

[DbContext(typeof(CanonicalStateDbContext))]
[Migration("202609040003_AddActionApprovals")]
public sealed class AddActionApprovals : Migration
{
    private static readonly string[] ProjectRepositoryConsumedColumns =
    [
        "ProjectId",
        "RepositoryId",
        "ConsumedAtUnixMs",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ActionApprovals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                OwnerPrincipalReference = table.Column<string>(
                    type: "TEXT",
                    maxLength: 256,
                    nullable: false),
                ActionName = table.Column<string>(
                    type: "TEXT",
                    maxLength: 200,
                    nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                IntentFingerprint = table.Column<string>(
                    type: "TEXT",
                    maxLength: 128,
                    nullable: false),
                ApprovedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
                ExpiresAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
                ConsumedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: true),
                RevokedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ActionApprovals", approval => approval.Id);
                table.ForeignKey(
                    name: "FK_ActionApprovals_Projects_ProjectId",
                    column: approval => approval.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ActionApprovals_Repositories_RepositoryId",
                    column: approval => approval.RepositoryId,
                    principalTable: "Repositories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ActionApprovals_ProjectId",
            table: "ActionApprovals",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_ActionApprovals_RepositoryId",
            table: "ActionApprovals",
            column: "RepositoryId");

        migrationBuilder.CreateIndex(
            name: "IX_ActionApprovals_ProjectId_RepositoryId_ConsumedAtUnixMs",
            table: "ActionApprovals",
            columns: ProjectRepositoryConsumedColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ActionApprovals");
    }
}
