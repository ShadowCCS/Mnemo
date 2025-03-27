using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MnemoProject.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitContentToUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnitContent",
                table: "Units",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitContent",
                table: "Units");
        }
    }
}
