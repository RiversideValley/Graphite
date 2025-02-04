﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Riverside.Graphite.Data.Core.Migrations;
public partial class IntialCreate : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		_ = migrationBuilder.CreateTable(
			name: "Urls",
			columns: table => new
			{
				id = table.Column<int>(type: "INTEGER", nullable: false)
					.Annotation("Sqlite:Autoincrement", true),
				last_visit_time = table.Column<string>(type: "TEXT", nullable: true),
				url = table.Column<string>(type: "TEXT", nullable: true),
				title = table.Column<string>(type: "TEXT", nullable: true),
				visit_count = table.Column<int>(type: "INTEGER", nullable: false),
				typed_count = table.Column<int>(type: "INTEGER", nullable: false),
				hidden = table.Column<int>(type: "INTEGER", nullable: false)
			},
			constraints: table =>
			{
				_ = table.PrimaryKey("PK_Urls", x => x.id);
			});
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		_ = migrationBuilder.DropTable(
			name: "Urls");
	}

}