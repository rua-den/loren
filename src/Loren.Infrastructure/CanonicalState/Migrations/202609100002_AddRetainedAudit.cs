using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Loren.Infrastructure.CanonicalState.Migrations;

[DbContext(typeof(CanonicalStateDbContext))]
[Migration("202609100002_AddRetainedAudit")]
public sealed class AddRetainedAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditEvents",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                RunId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ActionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Kind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ActionName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Outcome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Detail = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                OccurredAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_AuditEvents", p => p.Id));
        migrationBuilder.CreateIndex("IX_AuditEvents_OccurredAtUnixMs", "AuditEvents", "OccurredAtUnixMs");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "AuditEvents");
}
