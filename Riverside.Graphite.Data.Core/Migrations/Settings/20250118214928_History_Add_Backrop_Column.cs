using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Riverside.Graphite.Data.Core.Migrations.Settings
{
    /// <inheritdoc />
    public partial class History_Add_Backrop_Column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "BackDrop",
				table: "Settings",
				type: "TEXT",
				defaultValue: "Mica",
				nullable: true);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "BackDrop",
				table: "Settings");
		}
        
    }
}
