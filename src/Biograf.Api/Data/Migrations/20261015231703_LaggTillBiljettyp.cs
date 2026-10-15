using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biograf.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LaggTillBiljettyp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bokningar som gjordes innan biljettyper fanns räknas som vuxenbiljetter.
            migrationBuilder.AddColumn<string>(
                name: "Biljettyp",
                table: "BokadePlatser",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Vuxen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Biljettyp",
                table: "BokadePlatser");
        }
    }
}
