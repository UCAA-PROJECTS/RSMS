using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSMS.Migrations
{
    /// <inheritdoc />
    public partial class Battery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IntrusionDetected",
                table: "Readings",
                newName: "ShelterAccess");

            migrationBuilder.AddColumn<double>(
                name: "Battery",
                table: "Readings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Stabilizer",
                table: "Readings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Battery",
                table: "Readings");

            migrationBuilder.DropColumn(
                name: "Stabilizer",
                table: "Readings");

            migrationBuilder.RenameColumn(
                name: "ShelterAccess",
                table: "Readings",
                newName: "IntrusionDetected");
        }
    }
}
