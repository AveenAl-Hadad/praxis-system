using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Entities;
using Praxis.Application.Interfaces;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Services;
/// <summary>
/// Service zur Berechnung von Dashboard-Statistiken.
/// Liefert aggregierte Daten wie Anzahl von Patienten, Umsatz usw.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly PraxisDbContext _db;

    /// <summary>
    /// Konstruktor mit Dependency Injection für den DbContext.
    /// </summary>
    public DashboardService(PraxisDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Berechnet und liefert die Dashboard-Statistiken.
    /// </summary>
    public async Task<DashboardStats> GetStatsAsync()
    {
        var now = DateTime.Now;

        // Monatsanfang und Monatsende bestimmen
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        // Alle Rechnungen laden (für Gesamtumsatz)
        var totalInvoices = await _db.Invoices.ToListAsync();

        // Rechnungen des aktuellen Monats laden
        var currentMonthInvoices = await _db.Invoices
            .Where(i => i.InvoiceDate >= startOfMonth && i.InvoiceDate < endOfMonth)
            .ToListAsync();

        // Dashboard-Daten zusammenstellen
        var stats = new DashboardStats
        {
            // Gesamtzahlen
            TotalPatients = await _db.Patients.CountAsync(),
            TotalAppointments = await _db.Appointments.CountAsync(),
            TotalInvoices = totalInvoices.Count,
            TotalPrescriptions = await _db.Prescriptions.CountAsync(),

            // Gesamtumsatz
            TotalRevenue = totalInvoices.Sum(i => i.TotalAmount),

            // Monatswerte
            CurrentMonthAppointments = await _db.Appointments
                .CountAsync(a =>
                    a.StartTime >= startOfMonth &&
                    a.StartTime < endOfMonth),

            CurrentMonthInvoices = currentMonthInvoices.Count,
            CurrentMonthRevenue = currentMonthInvoices.Sum(i => i.TotalAmount)
        };

        return stats;
    }
}