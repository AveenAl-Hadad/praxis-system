using System;
using System.Collections.Generic;
using System.ComponentModel;
using Praxis.Client.Logic;
using Praxis.Domain.Entities;
using Xunit;

namespace Praxis.Tests;
/// <summary>
/// Eine Testklasse = Sammlung von Testfällen, die zusammen ein Thema testen: hier PatientListManager.
/// </summary>
public class PatientListManagerTests
{
    /// <summary>
    /// Ziel: Viele Patienten automatisch erzeugen (Dummy-Daten), statt alles manuell zu schreiben.
    /// </summary>
    /// <param name="count"></param>
    /// <returns>Patient List</returns>
    private static List<Patient> CreatePatients(int count)
    {
        var list = new List<Patient>();
        for (int i = 1; i <= count; i++)
        {
            list.Add(new Patient
            {
                Id = i,
                Nachname = i % 2 == 0 ? "Müller" : "Schmidt", //alle geraden = Müller, alle ungeraden = Schmidt (gut fürs Filtern/Suchen/Sortieren)
                Vorname = "Vorname" + i,
                Email = $"p{i}@test.de", //jede Email eindeutig
                Telefonnummer = "123",
                Geburtsdatum = new DateTime(1990, 1, 1),
                IsActive = i % 3 != 0 //jeder 3. Patient inaktiv (3, 6, 9, …)
            });
        }
        return list;
    }
    /// <summary>
    /// OnlyActive Filter gibt ausschließlich aktive Patienten zurück.
    /// </summary>
    [Fact]
    public void Filter_OnlyActive_ReturnsOnlyActivePatients()
    {
        //Arrange (Vorbreiten)
        // ich erzeuge 10 Patienten.Dann erstelle ich den Manger mit diesem Liste
        var patients = CreatePatients(10);
        var mgr = new PatientListManager(patients);

        //Act(ausführen)
        // hier sag ich Suchtext:leer und onlyActive: true -- nur aktive sollen bleiben
        var result = mgr.GetFilteredAndSorted(searchTerm: "", onlyActive: true);

        //Assert (Prüfen)
        //Das bedeutet: für jedes Element im Ergebnis muss IsActive == true sein.
        Assert.All(result, p => Assert.True(p.IsActive));
    }
    /// <summary>
    /// Suche findet Nachname
    /// </summary>
    [Fact]
    public void Search_FindsByLastName()
    {
        //Arrange (Vorbreiten)
        //ich baue bewusst nur 2 Patienten, damit das Ergebnis eindeutig ist.
        var patients = new List<Patient>
        {
            new Patient { Id=1, Nachname="Mustermann", Vorname="Max", IsActive=true, Geburtsdatum=new DateTime(1980,1,1)},
            new Patient { Id=2, Nachname="Schneider", Vorname="Anna", IsActive=true, Geburtsdatum=new DateTime(1980,1,1)}
        };
        var mgr = new PatientListManager(patients);

        //Act(ausführen)
        //ich suche nach "muster" (klein geschrieben), erarte aber, dass"Mustermann" gefunden wird.
        //➡️ Damit testest du indirekt auch Case-insensitive Suche (wenn dein Manager .ToLower() nutzt).

        var result = mgr.GetFilteredAndSorted("muster", onlyActive: false);

        //Single = Ergebnis muss genau 1 sein.Und es muss Mustermann sein.
        Assert.Single(result);
        Assert.Equal("Mustermann", result[0].Nachname);
    }
    /// <summary>
    /// Sotierung Nachname A-->Z
    /// </summary>
    [Fact]
    public void Sorting_ByLastName_Ascending_Works()
    {
        /*Arrange
         * ich setze Sortierung explizit auf:
         * SortBy="Nachname"
         * Direction = Ascending
         */        
        var patients = new List<Patient>
        {
            new Patient { Id=1, Nachname="Zebra", Vorname="A", IsActive=true, Geburtsdatum=new DateTime(1980,1,1)},
            new Patient { Id=2, Nachname="Alpha", Vorname="B", IsActive=true, Geburtsdatum=new DateTime(1980,1,1)}
        };

        var mgr = new PatientListManager(patients);
        mgr.SetSorting(nameof(Patient.Nachname), ListSortDirection.Ascending);
        
        // Act
         var result = mgr.GetFilteredAndSorted("", false);
        
        //Assert
        Assert.Equal("Alpha", result[0].Nachname);
        Assert.Equal("Zebra", result[1].Nachname);
    }
    /// <summary>
    ///Pagination _ erste Seite hat 50 Items 
    /// </summary>
    [Fact]
    public void Pagination_Returns50ItemsOnFirstPage_WhenPageSize50()
    {
        /* Arrnage
         * 120 Patienten
         * PageSize = 50
         * Sort nach Id (damit Seite 1 IDs 1..50 sind)
         */
        var patients = CreatePatients(120);
        var mgr = new PatientListManager(patients) { PageSize = 50 };
        mgr.SetSorting(nameof(Patient.Id), ListSortDirection.Ascending);

        // Act
        //ich hole Seite 1.
        var page1 = mgr.GetPage("", false);

        /* Assert
         * 120/50 = 2,4 --> 3 Seiten
         * Seite 1: 50
         * Seite 2: 50
         * Seite 3: 20
         */
        Assert.Equal(50, page1.Count);
        Assert.Equal(1, mgr.CurrentPage);
        Assert.Equal(3, mgr.TotalPages); // 120 -> 3 Seiten (50,50,20)
    }
    /// <summary>
    /// NextPage geht weiter
    /// </summary>
    [Fact]
    public void Pagination_NextPage_MovesForward()
    {
        //Arrange
        //wie Vorher
        var patients = CreatePatients(120);
        var mgr = new PatientListManager(patients) { PageSize = 50 };
        mgr.SetSorting(nameof(Patient.Id), ListSortDirection.Ascending);

        /*Act
         * ich hole Seite 1, dann NextPage, dann Seite 2.
         */
        var page1 = mgr.GetPage("", false);
        mgr.NextPage();
        var page2 = mgr.GetPage("", false);

        /*Assert
         * CurrentPage muss 2 sein
         * Erstes Element Seite 1 ist nicht gleich erstes Element Seite 2
         * (weil Seite 1 bei Id=1 startet, Seite 2 bei Id=51)
         */
        Assert.Equal(2, mgr.CurrentPage);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }
    /// <summary>
    /// FilterChange rest auf seite 1
    /// </summary>
    [Fact]
    public void FilterChange_ShouldResetToFirstPage()
    {
        //Arrange
        //ich gehe absichtlich auf seite 2
        var patients = CreatePatients(120);
        var mgr = new PatientListManager(patients) { PageSize = 50 };
        mgr.SetSorting(nameof(Patient.Id), ListSortDirection.Ascending);

        mgr.GetPage("", false);
        mgr.NextPage();
        Assert.Equal(2, mgr.CurrentPage);
        //Act
        mgr.GoToFirstPage();
        //Assert
        Assert.Equal(1, mgr.CurrentPage);
    }
}