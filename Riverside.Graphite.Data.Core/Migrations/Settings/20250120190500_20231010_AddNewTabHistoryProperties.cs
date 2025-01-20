using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riverside.Graphite.Data.Core.Migrations.Settings
{
    /// <inheritdoc />
    public partial class AddTabHistoryProps_20231010 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.AddColumn<int>(
				name: "NewTabHistoryQuick",
				table: "Settings",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<int>(
				name: "NewTabHistoryDownloads",
				table: "Settings",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<int>(
				name: "NewTabHistoryFavorites",
				table: "Settings",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<int>(
				name: "NewTabHistoryHistory",
				table: "Settings",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);

			migrationBuilder.AddColumn<int>(
				name: "NewTabSelectorBarVisible",
				table: "Settings",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);
		}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropColumn(
			   name: "NewTabHistoryQuick",
			   table: "Settings");

			migrationBuilder.DropColumn(
				name: "NewTabHistoryDownloads",
				table: "Settings");

			migrationBuilder.DropColumn(
				name: "NewTabHistoryFavorites",
				table: "Settings");

			migrationBuilder.DropColumn(
				name: "NewTabHistoryHistory",
				table: "Settings");

			migrationBuilder.DropColumn(
				name: "NewTabSelectorBarVisible",
				table: "Settings");
		}
    }
}
