using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Session;
using Praxis.Client.Views.Pages.Patienten;
using Praxis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        public async Task ReloadPatientSearchPageAsync()
        {
            await _patientSearchPage.RefreshAsync();
        }
        public async Task<IEnumerable<Patient>> GetPatientsAsync()
        {
            return await _patientService.GetAllPatientsAsync();
        }
        public async Task CreatePatientAsync(Patient patient)
        {
            var userName = UserSession.CurrentUser?.Username ?? "system";
            await _patientService.AddPatientAsync(patient, userName);
        }
        public async Task UpdatePatientAysnc(Patient patient)
        {
            await _patientService.UpdatePatientAsync(patient);
        }
        public async void OpenEditPatientPage(Patient patient)
        {
            LoadPage(_patientEditPage);
            await _patientEditPage.LoadPatientAsync(patient);
        }
        public async Task DeletePatientByIdAsync(int patientId)
        {
            var userName = UserSession.CurrentUser?.Username ?? "system";
            await _patientService.DeletePatientAsync(patientId, userName);
        }
        public async Task OpenPatientDocumentsPageAsync(Patient patient)
        {
            LoadPage(_patientDocumentsPage);
            await _patientDocumentsPage.LoadPatientAsync(patient);
        }
        public async Task OpenPatientAppointmentsPageAsync(Patient patient)
        {
            LoadPage(_patientAppointmentsPage);
            await _patientAppointmentsPage.LoadPatientAsync(patient);
        }
        public async Task OpenSelectedPatientMedicalRecordPageAsync()
        {
            if (_selectedPatient == null)
            {
                MessageBox.Show("Bitte zuerst in der Patientensuche einen Patienten auswählen oder doppelt anklicken.");
                return;
            }

            await OpenPatientMedicalRecordPageAsync(_selectedPatient);
        }
        public async Task OpenPatientMedicalRecordPageAsync(Patient patient)
        {
            var page = _serviceProvider.GetRequiredService<PatientMedicalRecordPage>();
            LoadPage(page);
            await page.LoadPatientAsync(patient);
        }
        public async Task OpenSelectedPatientDocumentsPageAsync()
        {
            if (_selectedPatient == null)
            {
                MessageBox.Show("Bitte zuerst in der Patientensuche einen Patienten auswählen oder doppelt anklicken.");
                return;
            }

            await OpenPatientDocumentsPageAsync(_selectedPatient);
        }
        public async Task OpenSelectedPatientAppointmentsPageAsync()
        {
            if (_selectedPatient == null)
            {
                MessageBox.Show("Bitte zuerst in der Patientensuche einen Patienten auswählen oder doppelt anklicken.");
                return;
            }

            await OpenPatientAppointmentsPageAsync(_selectedPatient);
        }
        public async Task OpenLaborRecordPageAsync(int laborRecordId)
        {
            SwitchModule(BottomModule.Labor);
            LoadPage(_laborPage);
            await _laborPage.ShowLaborRecordAsync(laborRecordId);
        }
        public async Task OpenPatientMedicalRecordLaborEntryAsync(Patient patient, int laborRecordId)
        {
            var page = _serviceProvider.GetRequiredService<PatientMedicalRecordPage>();

            LoadPage(page);

            await page.LoadPatientAndSelectLaborEntryAsync(patient, laborRecordId);
        }        //Patien Dokument
        public async Task<IEnumerable<PatientDocument>> GetDocumentsByPatientIdAsync(int patientId)
        {
            return await _documentService.GetDocumentsByPatientAsync(patientId);
        }
        public async Task AddDocumentAsync(PatientDocument document)
        {
            await _documentService.AddDocumentAsync(document);
        }
        public async Task UpdateDocumentAsync(PatientDocument document)
        {
            await _documentService.UpdateDocumentAsync(document);
        }
        public async Task DeleteDocumentAsync(int documentId)
        {
            await _documentService.DeleteDocumentAsync(documentId);
        }
        //
        public async Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            var allAppointments = await _appointmentService.GetAllAppointmentsAsync();
            return allAppointments
                .Where(a => a.PatientId == patientId)
                .OrderBy(a => a.StartTime)
                .ToList();
        }
        public void SetSelectedPatient(Patient patient)
        {
            _selectedPatient = patient;
        }
        public Patient? GetSelectedPatient()
        {
            return _selectedPatient;
        }

    }
}
