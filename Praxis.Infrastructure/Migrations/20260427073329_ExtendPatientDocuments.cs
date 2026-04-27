using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Praxis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendPatientDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientDocuments_PatientId",
                table: "PatientDocuments");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PatientDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PatientDocuments",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "PatientDocuments",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "PatientDocuments",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_PatientId_CreatedAt",
                table: "PatientDocuments",
                columns: new[] { "PatientId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientDocuments_PatientId_CreatedAt",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "PatientDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_PatientId",
                table: "PatientDocuments",
                column: "PatientId");
        }
    }
}
