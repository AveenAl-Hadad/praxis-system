using System.Windows;
using MessageBox = System.Windows.MessageBox;
namespace Praxis.Client.Logic.UI;

public class WpfMessageBoxService : IMessageBoxService
{
    // Zeigt eine Bestätigungs-MessageBox (Ja/Nein)
    public MessageBoxResult Confirm(string message, string title)
        => MessageBox.Show(
            message,                     // Nachrichtentext
            title,                       // Titel der MessageBox
            MessageBoxButton.YesNo,      // Buttons: Ja / Nein
            MessageBoxImage.Warning);    // Warnsymbol

    // Zeigt eine Fehlermeldung (Error)
    public void ShowError(string message, string title)
        => MessageBox.Show(
            message,                     // Nachrichtentext
            title,                       // Titel
            MessageBoxButton.OK,         // Nur OK-Button
            MessageBoxImage.Error);      // Fehler-Symbol

    // Zeigt eine Informationsmeldung
    public void ShowInfo(string message, string title)
        => MessageBox.Show(
            message,                     // Nachrichtentext
            title,                       // Titel
            MessageBoxButton.OK,         // Nur OK-Button
            MessageBoxImage.Information // Info-Symbol
        );
}