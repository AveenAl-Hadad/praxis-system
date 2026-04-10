using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Praxis.Domain.Entities;
using MessageBox = System.Windows.MessageBox;
namespace Praxis.Client.Views.Pages.Patienten
{
    public partial class PatientDocumentsPage : System.Windows.Controls.UserControl
    {
        private Patient? _currentPatient;

        public PatientDocumentsPage()
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

            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                var documents = await mainWindow.GetDocumentsByPatientIdAsync(patient.Id);
                DocumentsGrid.ItemsSource = documents;
            }
        }

        private void AddDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Als Nächstes bauen wir Dokument neu anlegen direkt als Page oder Dialog.");
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                await mainWindow.OpenPatientSearchPageAsync();
            }
        }
    }
}