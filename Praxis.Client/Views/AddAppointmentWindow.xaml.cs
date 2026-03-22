using System.Globalization;
using System.Windows;
using Praxis.Domain.Entities;
using Praxis.Infrastructure.Services;
using Praxis.Infrastructure.Services.Interface;

namespace Praxis.Client.Views;

/// <summary>
/// Dieses Fenster dient zum Erstellen und Bearbeiten von Terminen.
/// 
/// Der Benutzer kann:
/// - einen Patienten auswählen
/// - ein Datum und eine Uhrzeit eingeben
/// - die Dauer des Termins festlegen
/// - einen Grund eintragen
/// - einen Status auswählen
/// 
/// Wenn bereits ein Termin übergeben wurde, arbeitet das Fenster im Bearbeitungsmodus.
/// Andernfalls wird ein neuer Termin erstellt.
/// </summary>
public partial class AddAppointmentWindow : Window
{
    /// <summary>
    /// Service für das Speichern und Aktualisieren von Terminen.
    /// </summary>
    private readonly IAppointmentService _appointmentService;

    /// <summary>
    /// Service zum Laden aller Patienten.
    /// Wird benötigt, damit ein Patient für den Termin ausgewählt werden kann.
    /// </summary>
    private readonly IPatientService _patientService;

    /// <summary>
    /// Enthält den Termin, der bearbeitet werden soll.
    /// Ist dieser Wert null, wird ein neuer Termin angelegt.
    /// </summary>
    private Appointment? _editingAppointment;

    /// <summary>
    /// Konstruktor des Fensters.
    /// 
    /// Übergibt die benötigten Services und registriert das Loaded-Event,
    /// damit beim Öffnen des Fensters Patienten und Standardwerte geladen werden.
    /// </summary>
    /// <param name="appointmentService">Service für Terminoperationen.</param>
    /// <param name="patientService">Service zum Laden der Patienten.</param>
    public AddAppointmentWindow(
        IAppointmentService appointmentService,
        IPatientService patientService)
    {
        InitializeComponent();
        _appointmentService = appointmentService;
        _patientService = patientService;

        Loaded += AddAppointmentWindow_Loaded;
    }

    /// <summary>
    /// Wird automatisch ausgeführt, wenn das Fenster geladen wurde.
    /// 
    /// Aufgaben dieser Methode:
    /// - Patienten aus der Datenbank laden
    /// - Statuswerte in die ComboBox einfügen
    /// - bei Bearbeitung vorhandene Termindaten ins Formular setzen
    /// - bei neuem Termin Standardwerte setzen
    /// </summary>
    /// <param name="sender">Das auslösende Objekt.</param>
    /// <param name="e">Eventdaten des Loaded-Events.</param>
    private async void AddAppointmentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Alle Patienten laden und in der ComboBox anzeigen
        var patients = await _patientService.GetAllPatientsAsync();
        PatientComboBox.ItemsSource = patients.ToList();

        // Mögliche Terminstatus laden
        StatusComboBox.ItemsSource = new List<string>
        {
            "Geplant",
            "Bestätigt",
            "Abgesagt",
            "Erledigt"
        };

        // Wenn ein bestehender Termin bearbeitet wird,
        // werden die vorhandenen Daten in die Eingabefelder übernommen
        if (_editingAppointment != null)
        {
            PatientComboBox.SelectedValue = _editingAppointment.PatientId;
            AppointmentDatePicker.SelectedDate = _editingAppointment.StartTime.Date;
            TimeTextBox.Text = _editingAppointment.StartTime.ToString("HH:mm");
            DurationTextBox.Text = _editingAppointment.DurationMinutes.ToString();
            ReasonTextBox.Text = _editingAppointment.Reason;

            // Vorhandenen Status anzeigen
            StatusComboBox.SelectedItem = _editingAppointment.Status;
        }
        else
        {
            // Standardwerte für einen neuen Termin setzen
            AppointmentDatePicker.SelectedDate = DateTime.Today;
            StatusComboBox.SelectedItem = "Geplant";
        }
    }

    /// <summary>
    /// Wird aufgerufen, wenn der Benutzer auf "Speichern" klickt.
    /// 
    /// Die Methode prüft zuerst alle Eingaben:
    /// - Patient muss ausgewählt sein
    /// - Datum muss vorhanden sein
    /// - Uhrzeit muss im Format HH:mm eingegeben werden
    /// - Dauer muss eine positive Zahl sein
    /// - Grund darf nicht leer sein
    /// 
    /// Danach wird entweder:
    /// - ein neuer Termin erstellt und gespeichert
    /// oder
    /// - ein bestehender Termin aktualisiert
    /// </summary>
    /// <param name="sender">Das auslösende Objekt.</param>
    /// <param name="e">Eventdaten des Click-Events.</param>
    private async void SaveAppointment_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Prüfen, ob ein Patient ausgewählt wurde
            if (PatientComboBox.SelectedItem is not Patient selectedPatient)
            {
                MessageBox.Show("Bitte Patient auswählen.");
                return;
            }

            // Prüfen, ob ein Datum ausgewählt wurde
            if (AppointmentDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Bitte Datum auswählen.");
                return;
            }

            // Prüfen, ob die Uhrzeit korrekt im Format HH:mm eingegeben wurde
            if (!TimeSpan.TryParseExact(TimeTextBox.Text, @"hh\:mm", null, out var time))
            {
                MessageBox.Show("Bitte Uhrzeit im Format HH:mm eingeben.");
                return;
            }

            // Prüfen, ob die Dauer gültig ist
            if (!int.TryParse(DurationTextBox.Text, out var duration) || duration <= 0)
            {
                MessageBox.Show("Bitte gültige Dauer eingeben.");
                return;
            }

            // Prüfen, ob ein Grund eingegeben wurde
            if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
            {
                MessageBox.Show("Bitte Grund eingeben.");
                return;
            }

            // Aus Datum und Uhrzeit den vollständigen Startzeitpunkt erstellen
            var startTime = AppointmentDatePicker.SelectedDate.Value.Date.Add(time);

            // Gewählten Status auslesen, Standard ist "Geplant"
            var selectedStatus = StatusComboBox.SelectedItem?.ToString() ?? "Geplant";

            // Wenn kein Bearbeitungstermin vorhanden ist,
            // wird ein neuer Termin erstellt
            if (_editingAppointment == null)
            {
                var newAppointment = new Appointment
                {
                    PatientId = selectedPatient.Id,
                    StartTime = startTime,
                    DurationMinutes = duration,
                    Reason = ReasonTextBox.Text.Trim(),
                    Status = selectedStatus
                };

                await _appointmentService.AddAppointmentAsync(newAppointment);
                MessageBox.Show("Termin wurde gespeichert.");

                // Dashboard im Hauptfenster aktualisieren
                await ((MainWindow)Application.Current.MainWindow).LoadDashboardAsync();
            }
            else
            {
                // Vorhandenen Termin mit neuen Werten aktualisieren
                _editingAppointment.PatientId = selectedPatient.Id;
                _editingAppointment.StartTime = startTime;
                _editingAppointment.DurationMinutes = duration;
                _editingAppointment.Reason = ReasonTextBox.Text.Trim();
                _editingAppointment.Status = selectedStatus;

                await _appointmentService.UpdateAppointmentAsync(_editingAppointment);
                MessageBox.Show("Termin wurde aktualisiert.");
            }

            // Fenster mit Erfolg schließen
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            // Fehlermeldung anzeigen, falls beim Speichern ein Fehler auftritt
            MessageBox.Show(ex.Message);
        }
    }

    /// <summary>
    /// Setzt einen vorhandenen Termin in das Fenster,
    /// damit dieser bearbeitet werden kann.
    /// 
    /// Außerdem wird der Fenstertitel angepasst.
    /// </summary>
    /// <param name="appointment">Der Termin, der bearbeitet werden soll.</param>
    public void SetAppointmentForEdit(Appointment appointment)
    {
        _editingAppointment = appointment;
        Title = "Termin bearbeiten";
    }
}