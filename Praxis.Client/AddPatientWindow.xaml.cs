using System;
using System.Windows;
using Praxis.Domain.Entities;

namespace Praxis.Client;

public partial class AddPatientWindow : Window
{
    public Patient? CreatedPatient { get; private set; }

    public AddPatientWindow()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var lastName = (LastNameBox.Text ?? "").Trim();
        var firstName = (FirstNameBox.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(lastName))
        {
            MessageBox.Show("Nachname ist ein Pflichtfeld.", "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
            LastNameBox.Focus();
            return;
        }

        CreatedPatient = new Patient
        {
            Vorname = firstName,
            Nachname = lastName,
            Geburtsdatum = DobPicker.SelectedDate ?? DateTime.MinValue,
            Email = (EmailBox.Text ?? "").Trim(),
            Telefonnummer = (PhoneBox.Text ?? "").Trim()
        };

        DialogResult = true;
        Close();
    }
}