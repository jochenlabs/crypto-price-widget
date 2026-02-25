using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoPriceWidget.Services;

public record AppSettings(bool IsVertical = false, bool IsTopmost = true);

public class SettingsService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CryptoPriceWidget",
        "settings.json");

    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            var json = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings,
                new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, json).ConfigureAwait(false);
        }
        catch { }
    }
}
