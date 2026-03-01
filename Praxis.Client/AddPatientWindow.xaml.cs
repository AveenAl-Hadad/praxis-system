using System;
using System.Windows;
using Praxis.Domain.Entities;
using System.Text.RegularExpressions;


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
        if (!ValidateInputs(out var error))
        {
            MessageBox.Show(error, "Validierung", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var lastName = (LastNameBox.Text ?? "").Trim();
        var firstName = (FirstNameBox.Text ?? "").Trim();
        var email = (EmailBox.Text ?? "").Trim();
        var phone = (PhoneBox.Text ?? "").Trim();

        // 🔹 WICHTIG: Nur neues Objekt erstellen wenn keines existiert
        if (CreatedPatient == null)
        
            CreatedPatient = new Patient();
        

        // 🔹 Werte setzen (für Add UND Edit)
        CreatedPatient.Vorname = firstName;
        CreatedPatient.Nachname = lastName;
        CreatedPatient.Geburtsdatum = DobPicker.SelectedDate!.Value;
        CreatedPatient.Email = string.IsNullOrWhiteSpace(email) ? null : email;
        CreatedPatient.Telefonnummer = string.IsNullOrWhiteSpace(phone) ? null : phone;

        DialogResult = true;
        Close();
    }
    private bool ValidateInputs(out string error)
    {
        error = "";

        var firstName = (FirstNameBox.Text ?? "").Trim();
        var lastName = (LastNameBox.Text ?? "").Trim();
        var email = (EmailBox.Text ?? "").Trim();
        var phone = (PhoneBox.Text ?? "").Trim();
        var dob = DobPicker.SelectedDate;

        if (string.IsNullOrWhiteSpace(firstName))
        {
            error = "Vorname ist ein Pflichtfeld.";
            FirstNameBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            error = "Nachname ist ein Pflichtfeld.";
            LastNameBox.Focus();
            return false;
        }

        if (!dob.HasValue)
        {
            error = "Geburtsdatum ist ein Pflichtfeld.";
            DobPicker.Focus();
            return false;
        }

        if (dob.Value.Date > DateTime.Today)
        {
            error = "Geburtsdatum darf nicht in der Zukunft liegen.";
            DobPicker.Focus();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            // einfache Email-Validierung (MVP)
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
            {
                error = "E-Mail ist ungültig.";
                EmailBox.Focus();
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            // erlaubt: Zahlen, +, Leerzeichen, -, (), /
            var phoneRegex = new Regex(@"^[0-9+\-\s()/]+$");
            if (!phoneRegex.IsMatch(phone))
            {
                error = "Telefonnummer enthält ungültige Zeichen.";
                PhoneBox.Focus();
                return false;
            }
        }

        return true;
    }
}