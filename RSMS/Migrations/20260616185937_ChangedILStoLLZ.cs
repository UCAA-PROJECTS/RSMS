using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RSMS.Migrations
{
    /// <inheritdoc />
    public partial class ChangedILStoLLZ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Shelters",
                keyColumn: "ShelterCode",
                keyValue: "ILS002");

            migrationBuilder.InsertData(
                table: "Shelters",
                columns: new[] { "ShelterCode", "ShelterName" },
                values: new object[] { "LLZ002", "LLZ Shelter" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Shelters",
                keyColumn: "ShelterCode",
                keyValue: "LLZ002");

            migrationBuilder.InsertData(
                table: "Shelters",
                columns: new[] { "ShelterCode", "ShelterName" },
                values: new object[] { "ILS002", "ILS Shelter" });
        }
    }
}
