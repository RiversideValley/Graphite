using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FireBrowserWinUi3DataCore.Migrations.Download;

/// <inheritdoc />
public partial class IntialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Downloads",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                guid = table.Column<string>(type: "TEXT", nullable: true),
                current_path = table.Column<string>(type: "TEXT", nullable: true),
                end_time = table.Column<string>(type: "TEXT", nullable: true),
                start_time = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Downloads", x => x.id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Downloads");
    }
}
