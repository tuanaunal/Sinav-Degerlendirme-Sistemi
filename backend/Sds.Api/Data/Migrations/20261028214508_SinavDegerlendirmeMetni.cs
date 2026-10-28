using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sds.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SinavDegerlendirmeMetni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DegerlendirmeMetni",
                table: "Sinavlar",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DegerlendirmeMetni",
                table: "Sinavlar");
        }
    }
}
