using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGONECompliance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundJobsMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundJobRuns",
                schema: "compliance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationWorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RelatedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedEvaluationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackgroundJobRuns_EvaluationWorkspaces_EvaluationWorkspaceId",
                        column: x => x.EvaluationWorkspaceId,
                        principalSchema: "compliance",
                        principalTable: "EvaluationWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobRuns_EvaluationWorkspaceId_Status_JobType",
                schema: "compliance",
                table: "BackgroundJobRuns",
                columns: new[] { "EvaluationWorkspaceId", "Status", "JobType" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobRuns_Status",
                schema: "compliance",
                table: "BackgroundJobRuns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobRuns",
                schema: "compliance");
        }
    }
}
