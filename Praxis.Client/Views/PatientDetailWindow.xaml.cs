using System.Windows;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster zeigt die Detailinformationen eines Patienten an.
/// 
/// Angezeigt werden:
/// - Name
/// - Geburtsdatum
/// - Alter
/// - E-Mail
/// - Telefonnummer
/// - Aktiv/Inaktiv-Status
/// </summary>
public partial class PatientDetailWindow : Window
{
    /// <summary>
    /// Konstruktor des Detailfensters.
    /// 
    /// Er übernimmt ein Patient-Objekt und zeigt dessen Daten
    /// in den Textfeldern des Fensters an.
    /// </summary>
    /// <param name="patient">Der Patient, dessen Details angezeigt werden sollen.</param>
    public PatientDetailWindow(Patient patient)
    {
        InitializeComponent();

        // Name des Patienten im Titel anzeigen
        TitleText.Text = $"{patient.Nachname}, {patient.Vorname}";

        // Weitere Detailinformationen anzeigen
        DobText.Text = $"Geburtsdatum: {patient.Geburtsdatum:d}";
        AgeText.Text = $"Alter: {patient.Alter}";
        EmailText.Text = $"E-Mail: {patient.Email ?? "-"}";
        PhoneText.Text = $"Telefon: {patient.Telefonnummer ?? "-"}";
        StatusText2.Text = $"Status: {(patient.IsActive ? "Aktiv" : "Inaktiv")}";
    }

    /// <summary>
    /// Schließt das Detailfenster.
    /// </summary>
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}