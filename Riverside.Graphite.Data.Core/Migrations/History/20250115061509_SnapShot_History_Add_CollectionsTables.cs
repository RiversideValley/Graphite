using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riverside.Graphite.Data.Core.Migrations.History
{
    /// <inheritdoc />
    public partial class SnapShot_History_Add_CollectionsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HistoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CollectionNameId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collections_CollectionNames_CollectionNameId",
                        column: x => x.CollectionNameId,
                        principalTable: "CollectionNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Collections_Urls_HistoryItemId",
                        column: x => x.HistoryItemId,
                        principalTable: "Urls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Collections_CollectionNameId",
                table: "Collections",
                column: "CollectionNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_HistoryItemId",
                table: "Collections",
                column: "HistoryItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Collections");

            migrationBuilder.DropTable(
                name: "CollectionNames");

        }
    }
}
