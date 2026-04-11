using Praxis.Application.Interfaces;
using System.Text.Json;

namespace Praxis.Infrastructure.Services;

public class DashboardLayoutService : IDashboardLayoutService
{
    private readonly string _baseFolder;

    public DashboardLayoutService()
    {
        _baseFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Praxis.Client");

        Directory.CreateDirectory(_baseFolder);
    }

    public async Task<List<string>> GetWidgetOrderAsync(string username)
    {
        var filePath = GetFilePath(username);

        if (!File.Exists(filePath))
            return GetDefaultOrder();

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var order = JsonSerializer.Deserialize<List<string>>(json);

            if (order == null || order.Count == 0)
                return GetDefaultOrder();

            return order;
        }
        catch
        {
            return GetDefaultOrder();
        }
    }

    public async Task SaveWidgetOrderAsync(string username, List<string> widgetOrder)
    {
        var filePath = GetFilePath(username);

        var json = JsonSerializer.Serialize(widgetOrder, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json);
    }

    private string GetFilePath(string username)
    {
        var safeUsername = MakeSafeFileName(username);
        return Path.Combine(_baseFolder, $"dashboard-layout-{safeUsername}.json");
    }

    private static string MakeSafeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "default";

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }

        return value.Trim().ToLowerInvariant();
    }

    private static List<string> GetDefaultOrder()
    {
        return new List<string>
        {
            "Stats",
            "Overview",
            "Tasks",
            "Notices",
            "Appointments"
        };
    }
}