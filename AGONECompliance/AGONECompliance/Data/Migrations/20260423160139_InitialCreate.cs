using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGONECompliance.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationWorkspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SystemPrompt = table.Column<string>(type: "TEXT", nullable: false),
                    UserPromptFormat = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationWorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    RequirementText = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceRules_EvaluationWorkspaces_EvaluationWorkspaceId",
                        column: x => x.EvaluationWorkspaceId,
                        principalTable: "EvaluationWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadedDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationWorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    BlobPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    FullText = table.Column<string>(type: "TEXT", nullable: true),
                    ParsedJson = table.Column<string>(type: "TEXT", nullable: true),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessingError = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedDocuments_EvaluationWorkspaces_EvaluationWorkspaceId",
                        column: x => x.EvaluationWorkspaceId,
                        principalTable: "EvaluationWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationWorkspaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProspectusDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationRuns_EvaluationWorkspaces_EvaluationWorkspaceId",
                        column: x => x.EvaluationWorkspaceId,
                        principalTable: "EvaluationWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationRuns_UploadedDocuments_ProspectusDocumentId",
                        column: x => x.ProspectusDocumentId,
                        principalTable: "UploadedDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceExcerpt = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceLocation = table.Column<string>(type: "TEXT", nullable: false),
                    PageNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_ComplianceRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "ComplianceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluationResults_EvaluationRuns_EvaluationRunId",
                        column: x => x.EvaluationRunId,
                        principalTable: "EvaluationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationRunRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EvaluationRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRunRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationRunRules_EvaluationRuns_EvaluationRunId",
                        column: x => x.EvaluationRunId,
                        principalTable: "EvaluationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRules_EvaluationWorkspaceId_Code",
                table: "ComplianceRules",
                columns: new[] { "EvaluationWorkspaceId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRules_EvaluationWorkspaceId_IsActive",
                table: "ComplianceRules",
                columns: new[] { "EvaluationWorkspaceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_EvaluationRunId_RuleId",
                table: "EvaluationResults",
                columns: new[] { "EvaluationRunId", "RuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationResults_RuleId",
                table: "EvaluationResults",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRunRules_EvaluationRunId_RuleId",
                table: "EvaluationRunRules",
                columns: new[] { "EvaluationRunId", "RuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_EvaluationWorkspaceId_Status",
                table: "EvaluationRuns",
                columns: new[] { "EvaluationWorkspaceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRuns_ProspectusDocumentId",
                table: "EvaluationRuns",
                column: "ProspectusDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationWorkspaces_Name",
                table: "EvaluationWorkspaces",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_IsActive",
                table: "PromptTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_TemplateType_Version",
                table: "PromptTemplates",
                columns: new[] { "TemplateType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDocuments_EvaluationWorkspaceId_Type",
                table: "UploadedDocuments",
                columns: new[] { "EvaluationWorkspaceId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDocuments_Type",
                table: "UploadedDocuments",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationResults");

            migrationBuilder.DropTable(
                name: "EvaluationRunRules");

            migrationBuilder.DropTable(
                name: "PromptTemplates");

            migrationBuilder.DropTable(
                name: "ComplianceRules");

            migrationBuilder.DropTable(
                name: "EvaluationRuns");

            migrationBuilder.DropTable(
                name: "UploadedDocuments");

            migrationBuilder.DropTable(
                name: "EvaluationWorkspaces");
        }
    }
}
