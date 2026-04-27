using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Praxis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendLaborRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "LaborRecords",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientId",
                table: "LaborRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LaborRecords_PatientId",
                table: "LaborRecords",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_LaborRecords_Patients_PatientId",
                table: "LaborRecords",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LaborRecords_Patients_PatientId",
                table: "LaborRecords");

            migrationBuilder.DropIndex(
                name: "IX_LaborRecords_PatientId",
                table: "LaborRecords");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "LaborRecords");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "LaborRecords");
        }
    }
}
