using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGONECompliance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleGenerationJobAndAuthFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelatedRuleGenerationRequestId",
                schema: "compliance",
                table: "BackgroundJobRuns",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelatedRuleGenerationRequestId",
                schema: "compliance",
                table: "BackgroundJobRuns");
        }
    }
}
