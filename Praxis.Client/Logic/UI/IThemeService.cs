namespace Praxis.Client.Logic.UI;

public interface IThemeService
{
    void ApplyLightTheme();
    void ApplyDarkTheme();
    bool IsDarkMode { get; }
}