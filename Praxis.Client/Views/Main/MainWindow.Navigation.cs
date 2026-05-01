using Praxis.Client.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;
using MessageBox = System.Windows.MessageBox;

namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        private void LoadPage(UserControl page)
        {
            if (MainContentControl != null)
                MainContentControl.Content = page;
        }
        private void NavigateTo(UserControl page)
        {
            LoadPage(page);
        }       
        private void SwitchModule(BottomModule module)
        {
            _currentModule = module;
            BuildSidebarAsync(module);

            switch (module)
            {
                case BottomModule.Patienten:
                    LoadPage(_dashboardPage);
                    _ = _dashboardPage.RefreshAsync();
                    break;

                case BottomModule.Labor:
                    LoadPage(_laborPage);
                    break;

                case BottomModule.Abrechnung:
                    LoadPage(_abrechnungPage);
                    break;

                case BottomModule.Auswertungen:
                    LoadPage(_reportsPage);
                    _ = _reportsPage.RefreshAsync();
                    break;

                case BottomModule.Nachrichten:
                    LoadPage(_messagesPage);
                    _ = _messagesPage.RefreshAsync();
                    break;

                case BottomModule.Kataloge:
                    LoadPage(_catalogsPage);
                    break;

                case BottomModule.Einrichtung:
                    if (IsAdmin())
                        LoadPage(_userManagementPage);
                    else
                        LoadPlaceholderPage("Keine Berechteigung");
                    break;

                case BottomModule.Einstellungen:
                    LoadPage(_settingsPage);
                    break;

                default:
                    LoadPage(_patientSearchPage);
                    break;
            }
        }
        private void LoadPlaceholderPage(string title)
        {
            var grid = new Grid
            {
                Margin = new Thickness(16)
            };

            var text = new TextBlock
            {
                Text = $"{title} – Bereich folgt als Nächstes",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };

            grid.Children.Add(text);
            MainContentControl.Content = grid;
        }
        public async Task LoadDashboardAsync()
        {
            try
            {
                if (_currentModule == BottomModule.Patienten)
                {
                    LoadPage(_dashboardPage);
                    await _dashboardPage.RefreshAsync();
                    return;
                }

                switch (_currentModule)
                {
                    case BottomModule.Labor:
                        LoadPage(_laborPage);
                        break;

                    case BottomModule.Abrechnung:
                        LoadPage(_abrechnungPage);
                        break;

                    case BottomModule.Auswertungen:
                        LoadPage(_reportsPage);
                        break;

                    case BottomModule.Nachrichten:
                        LoadPage(_messagesPage);
                        break;

                    case BottomModule.Kataloge:
                        LoadPlaceholderPage("Kataloge");
                        break;

                    case BottomModule.Einrichtung:
                        LoadPlaceholderPage("Einrichtung");
                        break;

                    case BottomModule.Einstellungen:
                        LoadPlaceholderPage("Einstellungen");
                        break;

                    default:
                        LoadPage(_dashboardPage);
                        await _dashboardPage.RefreshAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Aktualisieren der Hauptansicht:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        public async Task OpenPatientSearchPageAsync()
        {
            LoadPage(_patientSearchPage);
            await _patientSearchPage.RefreshAsync();
        }
        public void OpenPatientCreatePage()
        {
            LoadPage(_patientCreatePage);
        }
        public async Task OpenPatientEditPageAsync()
        {
            if (_selectedPatient == null)
            {
                MessageBox.Show("Bitte zuerst einen Patienten auswählen.");
                return;
            }

            LoadPage(_patientEditPage);
            await _patientEditPage.LoadPatientAsync(_selectedPatient);
        }
    }
}
