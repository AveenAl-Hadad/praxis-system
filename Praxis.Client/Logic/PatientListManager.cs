using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Praxis.Domain.Entities;

namespace Praxis.Client.Logic;

/// <summary>
/// Verwaltet die Patientenliste für die UI:
/// - Filter (Suche / nur aktive)
/// - Sortierung (z.B. Nachname A→Z)
/// - Pagination (Seite 1..N)
///
/// Diese Klasse enthält KEIN WPF/UI-Code (kein DataGrid etc.).
/// MainWindow ruft nur Methoden auf und zeigt das Ergebnis an.
/// </summary>
public class PatientListManager
{
    private readonly List<Patient> _allPatients;

    // Sort Zustand
    private string _sortBy = nameof(Patient.Nachname);
    private ListSortDirection _sortDir = ListSortDirection.Ascending;

    // Pagination Zustand
    public int PageSize { get; set; } = 50;
    public int CurrentPage { get; private set; } = 1;
    public int TotalPages { get; private set; } = 1;

    public PatientListManager(List<Patient> allPatients)
    {
        _allPatients = allPatients ?? new List<Patient>();
    }

    /// <summary>
    /// Setzt die Sortierung. Wird z.B. vom DataGrid Header-Klick gesetzt.
    /// sortBy muss ein Property-Name von Patient sein (z.B. "Nachname", "Vorname", "Id"...)
    /// </summary>
    public void SetSorting(string sortBy, ListSortDirection direction)
    {
        if (!string.IsNullOrWhiteSpace(sortBy))
            _sortBy = sortBy;

        _sortDir = direction;
    }

    /// <summary>
    /// Springt auf die erste Seite (z.B. wenn Filter/Suche geändert wurde).
    /// </summary>
    public void GoToFirstPage() => CurrentPage = 1;

    /// <summary>
    /// Geht eine Seite weiter (wenn möglich).
    /// </summary>
    public void NextPage()
    {
        if (CurrentPage < TotalPages)
            CurrentPage++;
    }

    /// <summary>
    /// Geht eine Seite zurück (wenn möglich).
    /// </summary>
    public void PreviousPage()
    {
        if (CurrentPage > 1)
            CurrentPage--;
    }

    /// <summary>
    /// Gibt die aktuelle Seite zurück (Filter + Sort + Pagination).
    /// MainWindow setzt das Ergebnis als ItemsSource im DataGrid.
    /// </summary>
    public List<Patient> GetPage(string searchTerm, bool onlyActive)
    {
        var filteredSorted = GetFilteredAndSorted(searchTerm, onlyActive);

        // Pagination berechnen
        TotalPages = Math.Max(1, (int)Math.Ceiling(filteredSorted.Count / (double)PageSize));

        // Seite validieren
        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        // Nur aktuelle Seite holen
        return filteredSorted
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    /// <summary>
    /// Gibt die komplette Liste zurück:
    /// gefiltert + sortiert, aber OHNE Pagination.
    /// Das ist ideal für CSV Export.
    /// </summary>
    public List<Patient> GetFilteredAndSorted(string searchTerm, bool onlyActive)
    {
        IEnumerable<Patient> query = _allPatients;

        // 1) Filter: Nur aktive
        if (onlyActive)
            query = query.Where(p => p.IsActive);

        // 2) Filter: Suche (Nachname/Vorname/Email/Telefon)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();

            query = query.Where(p =>
                (p.Nachname ?? "").ToLower().Contains(term) ||
                (p.Vorname ?? "").ToLower().Contains(term) ||
                (p.Email ?? "").ToLower().Contains(term) ||
                (p.Telefonnummer ?? "").ToLower().Contains(term));
        }

        var filtered = query.ToList();

        // 3) Sortierung anwenden
        return SortInternal(filtered);
    }

    /// <summary>
    /// Sortiert eine Liste anhand des internen Sort-Zustands (_sortBy/_sortDir).
    /// Unterstützt die typischen Patient-Spalten.
    /// </summary>
    private List<Patient> SortInternal(List<Patient> list)
    {
        bool asc = _sortDir == ListSortDirection.Ascending;

        return _sortBy switch
        {
            nameof(Patient.Id) =>
                asc ? list.OrderBy(p => p.Id).ToList()
                    : list.OrderByDescending(p => p.Id).ToList(),

            nameof(Patient.Nachname) =>
                asc ? list.OrderBy(p => p.Nachname).ThenBy(p => p.Vorname).ToList()
                    : list.OrderByDescending(p => p.Nachname).ThenBy(p => p.Vorname).ToList(),

            nameof(Patient.Vorname) =>
                asc ? list.OrderBy(p => p.Vorname).ThenBy(p => p.Nachname).ToList()
                    : list.OrderByDescending(p => p.Vorname).ThenBy(p => p.Nachname).ToList(),

            nameof(Patient.Geburtsdatum) =>
                asc ? list.OrderBy(p => p.Geburtsdatum).ToList()
                    : list.OrderByDescending(p => p.Geburtsdatum).ToList(),

            nameof(Patient.Alter) =>
                asc ? list.OrderBy(p => p.Alter).ToList()
                    : list.OrderByDescending(p => p.Alter).ToList(),

            nameof(Patient.Email) =>
                asc ? list.OrderBy(p => p.Email).ToList()
                    : list.OrderByDescending(p => p.Email).ToList(),

            nameof(Patient.Telefonnummer) =>
                asc ? list.OrderBy(p => p.Telefonnummer).ToList()
                    : list.OrderByDescending(p => p.Telefonnummer).ToList(),

            nameof(Patient.IsActive) =>
                asc ? list.OrderBy(p => p.IsActive).ThenBy(p => p.Nachname).ToList()
                    : list.OrderByDescending(p => p.IsActive).ThenBy(p => p.Nachname).ToList(),

            // Fallback: keine Sortierung
            _ => list
        };
    }
}