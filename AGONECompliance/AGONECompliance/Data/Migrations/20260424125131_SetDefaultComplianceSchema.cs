using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGONECompliance.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultComplianceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "compliance");

            migrationBuilder.RenameTable(
                name: "UploadedDocuments",
                newName: "UploadedDocuments",
                newSchema: "compliance");

            migrationBuilder.RenameTable(
                name: "PromptTemplates",
                newName: "PromptTemplates",
                newSchema: "compliance");

            migrationBuilder.RenameTable(
                name: "EvaluationWorkspaces",
                newName: "EvaluationWorkspaces",
                newSchema: "compliance");

            migrationBuilder.RenameTable(
                name: "EvaluationRuns",
                newName: "EvaluationRuns",
                newSchema: "compliance");

            migrationBuilder.RenameTable(
                name: "EvaluationRunRules",
                newName: "EvaluationRunRules",
                newSchema: "compliance");

            migrationBuilder.RenameTable(
                name: "EvaluationResults",
                newName: "EvaluationResults",
                newSchema: "compliance");

            migrationBuilder.RenameTable(
                name: "ComplianceRules",
                newName: "ComplianceRules",
                newSchema: "compliance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "UploadedDocuments",
                schema: "compliance",
                newName: "UploadedDocuments");

            migrationBuilder.RenameTable(
                name: "PromptTemplates",
                schema: "compliance",
                newName: "PromptTemplates");

            migrationBuilder.RenameTable(
                name: "EvaluationWorkspaces",
                schema: "compliance",
                newName: "EvaluationWorkspaces");

            migrationBuilder.RenameTable(
                name: "EvaluationRuns",
                schema: "compliance",
                newName: "EvaluationRuns");

            migrationBuilder.RenameTable(
                name: "EvaluationRunRules",
                schema: "compliance",
                newName: "EvaluationRunRules");

            migrationBuilder.RenameTable(
                name: "EvaluationResults",
                schema: "compliance",
                newName: "EvaluationResults");

            migrationBuilder.RenameTable(
                name: "ComplianceRules",
                schema: "compliance",
                newName: "ComplianceRules");
        }
    }
}
