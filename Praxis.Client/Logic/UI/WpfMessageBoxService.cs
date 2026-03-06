using System.Windows;

namespace Praxis.Client.Logic.UI;

public class WpfMessageBoxService : IMessageBoxService
{
    public MessageBoxResult Confirm(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);

    public void ShowError(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string message, string title)
    
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    
}