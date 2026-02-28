using System;
using System.Windows;
using Praxis.Domain.Entities;

namespace Praxis.Client;

public partial class AddPatientWindow : Window
{
    public Patient? CreatedPatient { get; private set; }

    public AddPatientWindow(Patient? patient = null)
    {
        InitializeComponent();

        if (patient != null)
        {
            FirstNameBox.Text = patient.Vorname;
            LastNameBox.Text = patient.Nachname;
            DobPicker.SelectedDate = patient.Geburtsdatum;
            EmailBox.Text = patient.Email;
            PhoneBox.Text = patient.Telefonnummer;

            CreatedPatient = patient;
        }
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
            MessageBox.Show("Nachname ist ein Pflichtfeld.", "Validierung",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            LastNameBox.Focus();
            return;
        }

        // 🔹 WICHTIG: Nur neues Objekt erstellen wenn keines existiert
        if (CreatedPatient == null)
        {
            CreatedPatient = new Patient();
        }

        // 🔹 Werte setzen (für Add UND Edit)
        CreatedPatient.Vorname = firstName;
        CreatedPatient.Nachname = lastName;
        CreatedPatient.Geburtsdatum = DobPicker.SelectedDate ?? DateTime.Now;
        CreatedPatient.Email = (EmailBox.Text ?? "").Trim();
        CreatedPatient.Telefonnummer = (PhoneBox.Text ?? "").Trim();

        DialogResult = true;
        Close();
    }
}