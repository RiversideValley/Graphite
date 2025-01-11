using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riverside.Graphite.Data.Core.Migrations
{
    /// <inheritdoc />
    public partial class History_Add_Collections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HistoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Collection_Name = table.Column<string>(type: "TEXT", nullable: true),
                    DateTime_Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collections_Urls_HistoryItemId",
                        column: x => x.HistoryItemId,
                        principalTable: "Urls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
        }
    }
}
