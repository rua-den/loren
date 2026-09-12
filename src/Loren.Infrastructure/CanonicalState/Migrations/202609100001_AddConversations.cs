using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Loren.Infrastructure.CanonicalState.Migrations;

[DbContext(typeof(CanonicalStateDbContext))]
[Migration("202609100001_AddConversations")]
public sealed class AddConversations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Conversations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                OwnerPrincipalReference = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                ProjectAlias = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                CreatedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Conversations", p => p.Id));

        migrationBuilder.CreateTable(
            name: "ConversationMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ConversationId = table.Column<Guid>(type: "TEXT", nullable: false),
                Role = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAtUnixMs = table.Column<long>(type: "INTEGER", nullable: false),
                Sequence = table.Column<long>(type: "INTEGER", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConversationMessages", p => p.Id);
                table.ForeignKey("FK_ConversationMessages_Conversations_ConversationId", p => p.ConversationId, "Conversations", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Conversations_OwnerPrincipalReference_UpdatedAtUnixMs", "Conversations", ["OwnerPrincipalReference", "UpdatedAtUnixMs"]);
        migrationBuilder.CreateIndex("IX_ConversationMessages_ConversationId_Sequence", "ConversationMessages", ["ConversationId", "Sequence"], unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ConversationMessages");
        migrationBuilder.DropTable(name: "Conversations");
    }
}
