using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGONECompliance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExperionConversationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExperionConversationEntries",
                schema: "compliance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkspaceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserPrompt = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    AssistantResponse = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    ResponseLayer = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CacheKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperionConversationEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExperionConversationEntries_ConversationId_OccurredAtUtc",
                schema: "compliance",
                table: "ExperionConversationEntries",
                columns: new[] { "ConversationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExperionConversationEntries_SessionId",
                schema: "compliance",
                table: "ExperionConversationEntries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperionConversationEntries_UserId_ProductCode_WorkspaceId_OccurredAtUtc",
                schema: "compliance",
                table: "ExperionConversationEntries",
                columns: new[] { "UserId", "ProductCode", "WorkspaceId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExperionConversationEntries",
                schema: "compliance");
        }
    }
}
