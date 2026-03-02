using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RSMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shelters",
                columns: table => new
                {
                    ShelterCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShelterName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shelters", x => x.ShelterCode);
                });

            migrationBuilder.CreateTable(
                name: "Readings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShelterCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    Humidity = table.Column<double>(type: "float", nullable: false),
                    SmokeDetected = table.Column<bool>(type: "bit", nullable: false),
                    IntrusionDetected = table.Column<bool>(type: "bit", nullable: false),
                    WaterLeakDetected = table.Column<bool>(type: "bit", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Readings_Shelters_ShelterCode",
                        column: x => x.ShelterCode,
                        principalTable: "Shelters",
                        principalColumn: "ShelterCode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Shelters",
                columns: new[] { "ShelterCode", "ShelterName" },
                values: new object[,]
                {
                    { "DVOR003", "DVOR Shelter" },
                    { "GP001", "GP Shelter" },
                    { "ILS002", "ILS Shelter" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Readings_ShelterCode",
                table: "Readings",
                column: "ShelterCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Readings");

            migrationBuilder.DropTable(
                name: "Shelters");
        }
    }
}
