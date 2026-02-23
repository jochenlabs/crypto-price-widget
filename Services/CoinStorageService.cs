using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoPriceWidget.Services;

public record SavedCoin(
    string Id,
    string Symbol,
    string Name,
    decimal? LastPrice = null,
    DateTime? LastUpdatedAt = null);

public class CoinStorageService
{
    public static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CryptoPriceWidget",
        "coins.json");

    public async Task<List<SavedCoin>> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
                return GetDefaults();

            var json = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            var list = JsonSerializer.Deserialize<List<SavedCoin>>(json);
            return list?.Count > 0 ? list : GetDefaults();
        }
        catch
        {
            return GetDefaults();
        }
    }

    public async Task SaveAsync(IEnumerable<SavedCoin> coins)
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(coins,
            new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(FilePath, json).ConfigureAwait(false);
    }

    public static List<SavedCoin> GetDefaults() =>
    [
        new("bitcoin",  "BTC", "Bitcoin"),
        new("ethereum", "ETH", "Ethereum"),
        new("solana",   "SOL", "Solana")
    ];
}
