using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Loren.Infrastructure.CanonicalState.Migrations;

[DbContext(typeof(CanonicalStateDbContext))]
[Migration("202609070001_AddOrganizationItems")]
public sealed class AddOrganizationItems : Migration
{
    private static readonly string[] ProjectKindStatusColumns =
    [
        "ProjectId",
        "Kind",
        "TaskStatus",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OrganizationItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                TaskStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                SourceReference = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                CreatedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
                CompletedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrganizationItems", item => item.Id);
                table.ForeignKey(
                    name: "FK_OrganizationItems_Projects_ProjectId",
                    column: item => item.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OrganizationItems_ProjectId",
            table: "OrganizationItems",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_OrganizationItems_Kind",
            table: "OrganizationItems",
            column: "Kind");

        migrationBuilder.CreateIndex(
            name: "IX_OrganizationItems_TaskStatus",
            table: "OrganizationItems",
            column: "TaskStatus");

        migrationBuilder.CreateIndex(
            name: "IX_OrganizationItems_UpdatedAtUnixMs",
            table: "OrganizationItems",
            column: "UpdatedAtUnixMs");

        migrationBuilder.CreateIndex(
            name: "IX_OrganizationItems_ProjectId_Kind_TaskStatus",
            table: "OrganizationItems",
            columns: ProjectKindStatusColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OrganizationItems");
    }
}
