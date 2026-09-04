using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Loren.Infrastructure.CanonicalState.Migrations;

[DbContext(typeof(CanonicalStateDbContext))]
[Migration("202609040001_InitialCanonicalState")]
public sealed class InitialCanonicalState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Projects", project => project.Id);
            });

        migrationBuilder.CreateTable(
            name: "ProjectAliases",
            columns: table => new
            {
                NormalizedAlias = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Alias = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProjectAliases", alias => alias.NormalizedAlias);
                table.ForeignKey(
                    name: "FK_ProjectAliases_Projects_ProjectId",
                    column: alias => alias.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Repositories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ExternalNamespace = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ExternalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Repositories", repository => repository.Id);
                table.ForeignKey(
                    name: "FK_Repositories_Projects_ProjectId",
                    column: repository => repository.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProjectAliases_ProjectId",
            table: "ProjectAliases",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_Repositories_ProjectId",
            table: "Repositories",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_Repositories_Provider_ExternalNamespace_ExternalName",
            table: "Repositories",
            columns: new[] { "Provider", "ExternalNamespace", "ExternalName" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProjectAliases");
        migrationBuilder.DropTable(name: "Repositories");
        migrationBuilder.DropTable(name: "Projects");
    }
}
