using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Praxis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PracticeNotices",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PatientId",
                table: "PracticeNotices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticeNotices_PatientId",
                table: "PracticeNotices",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeNotices_Patients_PatientId",
                table: "PracticeNotices",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeNotices_Patients_PatientId",
                table: "PracticeNotices");

            migrationBuilder.DropIndex(
                name: "IX_PracticeNotices_PatientId",
                table: "PracticeNotices");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PracticeNotices");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "PracticeNotices");
        }
    }
}
