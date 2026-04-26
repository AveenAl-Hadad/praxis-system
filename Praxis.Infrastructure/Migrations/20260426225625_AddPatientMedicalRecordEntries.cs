using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Praxis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientMedicalRecordEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientMedicalRecordEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    AppointmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    EntryType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    IcdCode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    IcdText = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CatalogItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    LaborRecordId = table.Column<int>(type: "INTEGER", nullable: true),
                    PatientDocumentId = table.Column<int>(type: "INTEGER", nullable: true),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedicalRecordEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientMedicalRecordEntries_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientMedicalRecordEntries_CatalogItems_CatalogItemId",
                        column: x => x.CatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientMedicalRecordEntries_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientMedicalRecordEntries_LaborRecords_LaborRecordId",
                        column: x => x.LaborRecordId,
                        principalTable: "LaborRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientMedicalRecordEntries_PatientDocuments_PatientDocumentId",
                        column: x => x.PatientDocumentId,
                        principalTable: "PatientDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientMedicalRecordEntries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalRecordEntries_AppointmentId",
                table: "PatientMedicalRecordEntries",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalRecordEntries_CatalogItemId",
                table: "PatientMedicalRecordEntries",
                column: "CatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalRecordEntries_InvoiceId",
                table: "PatientMedicalRecordEntries",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalRecordEntries_LaborRecordId",
                table: "PatientMedicalRecordEntries",
                column: "LaborRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalRecordEntries_PatientDocumentId",
                table: "PatientMedicalRecordEntries",
                column: "PatientDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedicalRecordEntries_PatientId_EntryType_CreatedAt",
                table: "PatientMedicalRecordEntries",
                columns: new[] { "PatientId", "EntryType", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientMedicalRecordEntries");
        }
    }
}
