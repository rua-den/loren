using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Loren.Infrastructure.CanonicalState.Migrations;

[DbContext(typeof(CanonicalStateDbContext))]
[Migration("202609040002_AddMemoryRecords")]
public sealed class AddMemoryRecords : Migration
{
    private static readonly string[] ProjectCurrentColumns =
    [
        "ProjectId",
        "SupersededById",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MemoryRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SourceClass = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                RepositoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                SourceReference = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                SupersededById = table.Column<Guid>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MemoryRecords", memory => memory.Id);
                table.ForeignKey(
                    name: "FK_MemoryRecords_MemoryRecords_SupersededById",
                    column: memory => memory.SupersededById,
                    principalTable: "MemoryRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MemoryRecords_Projects_ProjectId",
                    column: memory => memory.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MemoryRecords_Repositories_RepositoryId",
                    column: memory => memory.RepositoryId,
                    principalTable: "Repositories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MemoryRecords_ProjectId_SupersededById",
            table: "MemoryRecords",
            columns: ProjectCurrentColumns);

        migrationBuilder.CreateIndex(
            name: "IX_MemoryRecords_RepositoryId",
            table: "MemoryRecords",
            column: "RepositoryId");

        migrationBuilder.CreateIndex(
            name: "IX_MemoryRecords_SupersededById",
            table: "MemoryRecords",
            column: "SupersededById");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MemoryRecords");
    }
}
