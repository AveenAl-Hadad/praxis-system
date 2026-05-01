using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using MouseEventHandler = System.Windows.Input.MouseEventHandler;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;

namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        private bool IsAdmin()
        {
            //return UserSession.HasRole(Roles.Administrator) || UserSession.HasRole("Admin");
            return PermissionHelper.IsAdmin;
        }
        private async Task BuildSidebarAsync(BottomModule module)
        {
            if (DynamicSidebarPanel == null)
                return;

            DynamicSidebarPanel.Children.Clear();
            _activeSidebarButton = null;

            switch (module)
            {
                case BottomModule.Patienten:
                    AddSidebarButton("Dashboard", async (s, e) => { LoadPage(_dashboardPage); await _dashboardPage.RefreshAsync(); }, true);
                    AddSidebarButton("Suche", async (s, e) => await OpenPatientSearchPageAsync(), true);
                    AddSidebarButton("Neuer Patient", (s, e) => OpenPatientCreatePage());
                    AddSidebarButton("Bearbeiten", async (s, e) => await OpenPatientEditPageAsync());

                    if (PermissionHelper.CanDeletePatients)
                    {
                        AddSidebarButton("Löschen", async (s, e) => await OpenPatientDeletePageAsync());
                    }
                    AddSidebarButton("Dokumente", async (s, e) => await OpenSelectedPatientDocumentsPageAsync());
                    AddSidebarButton("Termine", async (s, e) => await OpenSelectedPatientAppointmentsPageAsync());
                    AddSidebarButton("Karteikarte", async (s, e) => await OpenSelectedPatientMedicalRecordPageAsync());
                    AddSidebarButton("Wartezimmer", async (s, e) => { LoadPage(_waitingRoomPage); await _waitingRoomPage.RefreshAsync(); });
                    AddSidebarButton("Online-Buchung", OpenOnlineBooking_Click);
                    break;

                case BottomModule.Labor:
                    AddSidebarButton("Labordaten importieren", async (s, e) => { LoadPage(_laborPage); await _laborPage.ShowImportAsync(); }, true);
                    AddSidebarButton("Laborbücher zuordnen", async (s, e) => { LoadPage(_laborPage); await _laborPage.ShowLaborBooksAsync(); });
                    AddSidebarButton("Zugeordnete Laborberichte", async (s, e) => { LoadPage(_laborPage); await _laborPage.ShowAssignedReportsAsync(); });
                    AddSidebarButton("Labortagesliste", async (s, e) => { LoadPage(_laborPage); await _laborPage.ShowDailyListAsync(); });
                    AddSidebarButton("Labore", async (s, e) => { LoadPage(_laborPage); await _laborPage.ShowLabsAsync(); });
                    break;

                case BottomModule.Abrechnung:
                    AddSidebarButton("Neue KV-Abrechnung", async (s, e) => { LoadPage(_abrechnungPage); await _abrechnungPage.ShowNewKvAsync(); }, true);
                    AddSidebarButton("KV-Abrechnungen", async (s, e) => { LoadPage(_abrechnungPage); await _abrechnungPage.ShowKvAsync(); });
                    AddSidebarButton("Neue Privatabrechnung", async (s, e) => { LoadPage(_abrechnungPage); await _abrechnungPage.ShowNewPrivateAsync(); });
                    AddSidebarButton("Rechnungen", async (s, e) => { LoadPage(_abrechnungPage); await _abrechnungPage.ShowInvoicesAsync(); });
                    AddSidebarButton("Mahnungen", async (s, e) => { LoadPage(_abrechnungPage); await _abrechnungPage.ShowRemindersAsync(); });
                    break;

                case BottomModule.Auswertungen:
                    AddSidebarButton("Übersicht", (s, e) =>
                    {
                        LoadPage(_reportsPage);
                        _reportsPage.ShowOverview();
                    }, true);
                    AddSidebarButton("Patienten ohne Karte", (s, e) =>
                    {
                        LoadPage(_reportsPage);
                        _reportsPage.ShowPatientsWithoutCard();
                    });

                    AddSidebarButton("Leistungsziffern-Statistik", (s, e) =>
                    {
                        LoadPage(_reportsPage);
                        _reportsPage.ShowServiceCodeStats();
                    });

                    AddSidebarButton("Patienten-Statistik", (s, e) =>
                    {
                        LoadPage(_reportsPage);
                        _reportsPage.ShowPatientStats();
                    });

                    AddSidebarButton("Diagnose-Statistik", (s, e) =>
                    {
                        LoadPage(_reportsPage);
                        _reportsPage.ShowDiagnosisStats();
                    });

                    AddSidebarButton("Rechnungs-Statistik", (s, e) =>
                    {
                        LoadPage(_reportsPage);
                        _reportsPage.ShowInvoiceStats();
                    });

                    AddSidebarButton("Termin-Statistik", (s, e) =>
                    {
                        LoadPage(_reportsPage);
                        _reportsPage.ShowAppointmentStats();
                    });

                    AddSidebarButton("Diagramme", (s, e) =>
                    {
                        LoadPage(_reportsPage);
                        _reportsPage.ShowCharts();
                    });

                    break;

                case BottomModule.Nachrichten:

                    var unreadCount = await _messageService.GetUnreadCountAsync("MFA");

                    var inboxText = unreadCount > 0
                        ? $"🔴 Posteingang ({unreadCount})"
                        : "Posteingang";

                    AddSidebarButton(inboxText, async (s, e) =>
                    {
                        LoadPage(_messagesPage);
                        _messagesPage.ShowInbox();
                        await _messagesPage.RefreshAsync();

                        RefreshSidebarForCurrentModule();
                    }, true);

                    AddSidebarButton("Neue Nachricht", async (s, e) =>
                    {
                        LoadPage(_messagesPage);
                        _messagesPage.ShowNewMessage();
                        await _messagesPage.RefreshAsync();
                    }, true);

                    AddSidebarButton("Gesendet", async (s, e) =>
                    {
                        LoadPage(_messagesPage);
                        _messagesPage.ShowSent();
                        await _messagesPage.RefreshAsync();
                    });

                    AddSidebarButton("Externe Nachrichten", async (s, e) =>
                    {
                        LoadPage(_messagesPage);
                        _messagesPage.ShowExternalMessages();
                        await _messagesPage.RefreshAsync();
                    });

                    AddSidebarButton("Notizen", (s, e) =>
                    {
                        LoadPage(_messagesPage);
                        _messagesPage.ShowNotes();
                    });

                    AddSidebarButton("Arztbriefe", (s, e) =>
                    {
                        LoadPage(_messagesPage);
                        _messagesPage.ShowDoctorLetters();
                    });

                    break;

                case BottomModule.Kataloge:
                    AddSidebarButton("Katalogübersicht", async (s, e) => { LoadPage(_catalogsPage); await _catalogsPage.ShowOverviewAsync(); }, true);
                    AddSidebarButton("Diagnosen / ICD", async (s, e) => { LoadPage(_catalogsPage); await _catalogsPage.SelectCategoryAsync("Diagnosen / ICD"); });
                    AddSidebarButton("Leistungen / GOÄ / EBM", async (s, e) => { LoadPage(_catalogsPage); await _catalogsPage.SelectCategoryAsync("Leistungen / GOÄ / EBM"); });
                    AddSidebarButton("Medikamente", async (s, e) => { LoadPage(_catalogsPage); await _catalogsPage.SelectCategoryAsync("Medikamente"); });
                    AddSidebarButton("Formulare", async (s, e) => { LoadPage(_catalogsPage); await _catalogsPage.SelectCategoryAsync("Formulare"); });
                    AddSidebarButton("Dokumentvorlagen", async (s, e) => { LoadPage(_catalogsPage); await _catalogsPage.SelectCategoryAsync("Dokumentvorlagen"); }); break;

                case BottomModule.Einrichtung:
                    if (PermissionHelper.CanManageUsers)
                    {
                        AddSidebarButton("Benutzer", (s, e) => LoadPage(_userManagementPage), true);
                        AddSidebarButton("Arbeitsplätze", DummySidebarClick);
                        AddSidebarButton("TI-Konfiguration", DummySidebarClick);
                        AddSidebarButton("Behandler", async (s, e) => { LoadPage(_doctorsPage); await _doctorsPage.RefreshAsync(); });

                        AddSidebarButton("Räume", async (s, e) => { LoadPage(_roomsPage); await _roomsPage.RefreshAsync(); });
                        AddSidebarButton("Rollen", DummySidebarClick);
                    }
                    else
                    {
                        AddSidebarButton("Keine Berechtigung", DummySidebarClick, true);
                    }

                    break;

                case BottomModule.Einstellungen:
                    AddSidebarButton("Übersicht", (s, e) => LoadPage(_settingsPage), true);
                    AddSidebarButton("Design", DummySidebarClick);
                    AddSidebarButton("Passwort ändern", ChangePassword_Click);
                    AddSidebarButton("Backup", CreateBackup_Click);
                    AddSidebarButton("Restore", RestoreBackup_Click);
                    break;
            }
        }
        private async Task RefreshSidebarAsync()
        {
            SwitchModule(BottomModule.Nachrichten);
        }
        private void OpenOnlineBooking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = _serviceProvider.GetRequiredService<OnlineBookingWindow>();
                dialog.Owner = this;
                dialog.ShowDialog();

                _ = _dashboardPage.RefreshAsync();
                _ = _waitingRoomPage.RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Online-Buchung konnte nicht geöffnet werden:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void AddSidebarButton(string text, RoutedEventHandler clickHandler, bool setActive = false)
        {
            var btn = new Button
            {
                Content = text,
                Margin = new Thickness(0, 0, 0, 8),
                Style = TryFindResource("SidebarButtonStyle") as Style
            };

            btn.Click += (s, e) =>
            {
                SetActiveSidebarButton(btn);
                clickHandler?.Invoke(s, e);
            };

            DynamicSidebarPanel.Children.Add(btn);

            if (setActive)
                SetActiveSidebarButton(btn);
        }

        private void SetActiveSidebarButton(Button btn)
        {
            if (_activeSidebarButton != null)
            {
                var normalStyle = TryFindResource("SidebarButtonStyle") as Style;
                if (normalStyle != null)
                    _activeSidebarButton.Style = normalStyle;
            }

            _activeSidebarButton = btn;

            var activeStyle = TryFindResource("SidebarButtonActiveStyle") as Style;
            if (activeStyle != null)
                _activeSidebarButton.Style = activeStyle;
        }

        private void DummySidebarClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Diese Funktion baust du als Nächstes ein.",
                "Hinweis",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RefreshSidebarForCurrentModule()
        {
            BuildSidebarAsync(_currentModule);
        }
    }
}
