using System.Windows;
using Praxis.Domain.Entities;

namespace Praxis.Client;

public partial class PatientDetailWindow : Window
{
    public PatientDetailWindow(Patient patient)
    {
        InitializeComponent();

        TitleText.Text = $"{patient.Nachname}, {patient.Vorname}";
        DobText.Text = $"Geburtsdatum: {patient.Geburtsdatum:d}";
        AgeText.Text = $"Alter: {patient.Alter}";
        EmailText.Text = $"E-Mail: {patient.Email ?? "-"}";
        PhoneText.Text = $"Telefon: {patient.Telefonnummer ?? "-"}";
        StatusText2.Text = $"Status: {(patient.IsActive ? "Aktiv" : "Inaktiv")}";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}