using System.Windows;

namespace Praxis.Client.Logic.UI;

public class ThemeService : IThemeService
{
    public bool IsDarkMode { get; private set; }

    public void ApplyDarkTheme()
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri("Styles/DarkColors.xaml", UriKind.Relative)
        };

        ReplaceTheme(dict);
        IsDarkMode = true;
    }

    public void ApplyLightTheme()
    {
        var dict = new ResourceDictionary
        {
            Source = new Uri("Styles/Colors.xaml", UriKind.Relative)
        };

        ReplaceTheme(dict);
        IsDarkMode = false;
    }

    private void ReplaceTheme(ResourceDictionary newTheme)
    {
        var appResources = System.Windows.Application.Current.Resources;

        // Entferne alte Theme
        var existingTheme = appResources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Colors"));

        if (existingTheme != null)
            appResources.MergedDictionaries.Remove(existingTheme);

        // Neues Theme hinzufügen
        appResources.MergedDictionaries.Add(newTheme);
    }
}