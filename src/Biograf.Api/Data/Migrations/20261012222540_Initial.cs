using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biograf.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Filmer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Titel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Genre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Aldersgrans = table.Column<int>(type: "INTEGER", nullable: false),
                    LangdMinuter = table.Column<int>(type: "INTEGER", nullable: false),
                    Beskrivning = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    PosterUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Filmer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Salonger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Namn = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salonger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Saten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SalongId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rad = table.Column<int>(type: "INTEGER", nullable: false),
                    Nummer = table.Column<int>(type: "INTEGER", nullable: false),
                    Typ = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Saten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Saten_Salonger_SalongId",
                        column: x => x.SalongId,
                        principalTable: "Salonger",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Visningar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilmId = table.Column<int>(type: "INTEGER", nullable: false),
                    SalongId = table.Column<int>(type: "INTEGER", nullable: false),
                    Starttid = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Grundpris = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visningar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visningar_Filmer_FilmId",
                        column: x => x.FilmId,
                        principalTable: "Filmer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Visningar_Salonger_SalongId",
                        column: x => x.SalongId,
                        principalTable: "Salonger",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bokningar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Bokningsnummer = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    KundId = table.Column<string>(type: "TEXT", nullable: false),
                    VisningId = table.Column<int>(type: "INTEGER", nullable: false),
                    Skapad = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Totalpris = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bokningar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bokningar_Visningar_VisningId",
                        column: x => x.VisningId,
                        principalTable: "Visningar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BokadePlatser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BokningId = table.Column<int>(type: "INTEGER", nullable: false),
                    VisningId = table.Column<int>(type: "INTEGER", nullable: false),
                    SateId = table.Column<int>(type: "INTEGER", nullable: false),
                    Pris = table.Column<int>(type: "INTEGER", nullable: false),
                    Aktiv = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BokadePlatser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BokadePlatser_Bokningar_BokningId",
                        column: x => x.BokningId,
                        principalTable: "Bokningar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BokadePlatser_Saten_SateId",
                        column: x => x.SateId,
                        principalTable: "Saten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BokadePlatser_BokningId",
                table: "BokadePlatser",
                column: "BokningId");

            migrationBuilder.CreateIndex(
                name: "IX_BokadePlatser_SateId",
                table: "BokadePlatser",
                column: "SateId");

            migrationBuilder.CreateIndex(
                name: "IX_BokadePlatser_VisningId_SateId",
                table: "BokadePlatser",
                columns: new[] { "VisningId", "SateId" },
                unique: true,
                filter: "\"Aktiv\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Bokningar_Bokningsnummer",
                table: "Bokningar",
                column: "Bokningsnummer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bokningar_KundId",
                table: "Bokningar",
                column: "KundId");

            migrationBuilder.CreateIndex(
                name: "IX_Bokningar_VisningId",
                table: "Bokningar",
                column: "VisningId");

            migrationBuilder.CreateIndex(
                name: "IX_Filmer_Titel",
                table: "Filmer",
                column: "Titel");

            migrationBuilder.CreateIndex(
                name: "IX_Salonger_Namn",
                table: "Salonger",
                column: "Namn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Saten_SalongId_Rad_Nummer",
                table: "Saten",
                columns: new[] { "SalongId", "Rad", "Nummer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visningar_FilmId",
                table: "Visningar",
                column: "FilmId");

            migrationBuilder.CreateIndex(
                name: "IX_Visningar_SalongId",
                table: "Visningar",
                column: "SalongId");

            migrationBuilder.CreateIndex(
                name: "IX_Visningar_Starttid",
                table: "Visningar",
                column: "Starttid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BokadePlatser");

            migrationBuilder.DropTable(
                name: "Bokningar");

            migrationBuilder.DropTable(
                name: "Saten");

            migrationBuilder.DropTable(
                name: "Visningar");

            migrationBuilder.DropTable(
                name: "Filmer");

            migrationBuilder.DropTable(
                name: "Salonger");
        }
    }
}
