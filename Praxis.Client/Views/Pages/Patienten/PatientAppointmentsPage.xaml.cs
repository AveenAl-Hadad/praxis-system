using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;

namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class PatientAppointmentsPage : UserControl
    {
        private Patient? _currentPatient;

        public PatientAppointmentsPage()
        {
            InitializeComponent();
        }

        public async Task LoadPatientAsync(Patient patient)
        {
            _currentPatient = patient;

            PatientNameTextBox.Text = patient.FullName;
            GeburtsdatumTextBox.Text = patient.Geburtsdatum.ToString("dd.MM.yyyy");
            TelefonTextBox.Text = patient.Telefonnummer;
            EmailTextBox.Text = patient.Email;

            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                var appointments = await mainWindow.GetAppointmentsByPatientIdAsync(patient.Id);
                AppointmentsGrid.ItemsSource = appointments;
            }
        }

        private void AddAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Als Nächstes bauen wir Termin neu anlegen direkt als Page oder Dialog.");
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenPatientSearchPageAsync();
            }
        }
    }
}