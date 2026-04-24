using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AGONECompliance.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveParsedJsonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParsedJson",
                schema: "compliance",
                table: "UploadedDocuments");

            migrationBuilder.AlterColumn<string>(
                name: "ParsedJsonBlobPath",
                schema: "compliance",
                table: "UploadedDocuments",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ParsedJsonBlobPath",
                schema: "compliance",
                table: "UploadedDocuments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedJson",
                schema: "compliance",
                table: "UploadedDocuments",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
