using Praxis.Client.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        private void UpdateLoggedInUserDisplay()
        {
            try
            {
                var userName = UserSession.CurrentUser?.Username;
                var role = UserSession.CurrentUser?.Role;

                var displayName = !string.IsNullOrWhiteSpace(userName)
                    ? userName
                    : "Unbekannter Benutzer";

                LoggedInUserText.Text = displayName;
                LoggedInStatusText.Text = !string.IsNullOrWhiteSpace(role)
                    ? $"Angemeldet als {role}"
                    : "Angemeldet";

                UserInitialText.Text = GetInitials(displayName);
            }
            catch
            {
                LoggedInUserText.Text = "Benutzer";
                LoggedInStatusText.Text = "Angemeldet";
                UserInitialText.Text = "B";
            }
        }
        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
                return parts[0].Substring(0, 1).ToUpper();

            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }
        //Header Gehe zu
        //Event einbauen
        private void QuickSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                HandleQuickSearch(QuickSearchBox.Text);
            }
        }
        // Logik einbauen
        private void HandleQuickSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            input = input.Trim().ToLower();

            // Patienten
            if (input.Contains("pat") || input.Contains("max"))
            {
                _ = OpenPatientSearchWithTextAsync(input);
                return;
            }


            // Termine
            if (input.Contains("termin"))
            {
                SwitchModule(BottomModule.Patienten);
                _ = OpenSelectedPatientAppointmentsPageAsync();
                return;
            }

            // Abrechnung
            if (input.Contains("rechnung"))
            {
                SwitchModule(BottomModule.Abrechnung);
                return;
            }

            // Nachrichten
            if (input.Contains("nachricht"))
            {
                SwitchModule(BottomModule.Nachrichten);
                return;
            }

            // Auswertung
            if (input.Contains("stat") || input.Contains("auswertung"))
            {
                SwitchModule(BottomModule.Auswertungen);
                return;
            }

            _ = OpenPatientSearchWithTextAsync(input);
        }
        private async Task OpenPatientSearchWithTextAsync(string searchText)
        {
            SwitchModule(BottomModule.Patienten);

            LoadPage(_patientSearchPage);
            await _patientSearchPage.SetSearchAsync(searchText);
        }
    }
}
