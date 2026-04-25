using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGONECompliance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExperionMemoryCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExperionMemoryEntries",
                schema: "compliance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkspaceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MemoryKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    UserPrompt = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    AssistantResponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LayerSource = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HitCount = table.Column<int>(type: "int", nullable: false),
                    LastAccessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperionMemoryEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExperionMemoryEntries_LastAccessedAtUtc",
                schema: "compliance",
                table: "ExperionMemoryEntries",
                column: "LastAccessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ExperionMemoryEntries_MemoryKey",
                schema: "compliance",
                table: "ExperionMemoryEntries",
                column: "MemoryKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExperionMemoryEntries",
                schema: "compliance");
        }
    }
}
