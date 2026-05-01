using Microsoft.Extensions.DependencyInjection;
using Praxis.Client.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using MouseEventHandler = System.Windows.Input.MouseEventHandler;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;

namespace Praxis.Client.Views.Main
{
    public partial class MainWindow
    {
        private void StartSessionTimer()
        {
            _lastActivityTime = DateTime.Now;

            _sessionTimer = new DispatcherTimer();
            _sessionTimer.Interval = TimeSpan.FromSeconds(5);
            _sessionTimer.Tick += SessionTimer_Tick;
            _sessionTimer.Start();

            // Aktivität zuverlässig überwachen, auch wenn Controls Events selbst behandeln
            AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(ActivityDetected), true);
            AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(ActivityDetected), true);
            AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(ActivityDetected), true);
            AddHandler(UIElement.PreviewTextInputEvent, new TextCompositionEventHandler(ActivityDetected), true);
        }
        private void ActivityDetected(object sender, EventArgs e)
        {
            _lastActivityTime = DateTime.Now;
        }
        private void SessionTimer_Tick(object? sender, EventArgs e)
        {
            if (!UserSession.IsLoggedIn)
                return;

            var inactiveTime = DateTime.Now - _lastActivityTime;

            // Warnung 1 Minute vor Logout
            if (inactiveTime >= (_timeout - _warningTime) && inactiveTime < _timeout)
            {
                _sessionTimer.Stop();

                var result = MessageBox.Show(
                    $"Ihre Sitzung läuft in {_warningTime.Minutes} Minute(n) ab.\nMöchten Sie weiterarbeiten?",
                    "Session läuft ab",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _lastActivityTime = DateTime.Now;
                    _sessionTimer.Start();
                    return;
                }

                LogoutAuto();
                return;
            }

            // Logout nach kompletter Inaktivität
            if (inactiveTime >= _timeout)
            {
                _sessionTimer.Stop();
                LogoutAuto(true);
            }
        }
        private void LogoutAuto(bool showMessage = false)
        {
            _sessionTimer?.Stop();
            _warningTimer?.Stop();

            UserSession.Logout();

            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            System.Windows.Application.Current.MainWindow = loginWindow;
            loginWindow.Show();

            if (showMessage)
            {
                MessageBox.Show(
                    loginWindow,
                    "Sie wurden aufgrund von Inaktivität automatisch abgemeldet.",
                    "Session Timeout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            this.Close();
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie sich wirklich abmelden?",
                "Abmelden",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            _sessionTimer?.Stop();
            _warningTimer?.Stop();

            UserSession.Logout();

            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            System.Windows.Application.Current.MainWindow = loginWindow;
            loginWindow.Show();

            this.Close();
        }
    }
}
