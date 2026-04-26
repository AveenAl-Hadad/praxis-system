using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Praxis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentMedicalEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentMedicalEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppointmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    DiagnosisCatalogItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    ServiceCatalogItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentMedicalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentMedicalEntries_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentMedicalEntries_CatalogItems_DiagnosisCatalogItemId",
                        column: x => x.DiagnosisCatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppointmentMedicalEntries_CatalogItems_ServiceCatalogItemId",
                        column: x => x.ServiceCatalogItemId,
                        principalTable: "CatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentMedicalEntries_AppointmentId",
                table: "AppointmentMedicalEntries",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentMedicalEntries_DiagnosisCatalogItemId",
                table: "AppointmentMedicalEntries",
                column: "DiagnosisCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentMedicalEntries_ServiceCatalogItemId",
                table: "AppointmentMedicalEntries",
                column: "ServiceCatalogItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentMedicalEntries");
        }
    }
}
