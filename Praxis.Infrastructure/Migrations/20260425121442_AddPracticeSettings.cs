using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Praxis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PracticeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DoctorName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Street = table.Column<string>(type: "TEXT", nullable: false),
                    ZipCity = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeSettings");
        }
    }
}
