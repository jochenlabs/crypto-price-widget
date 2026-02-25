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
            if (list?.Count > 0)
            {
                // Migrate old CoinGecko IDs (lowercase/hyphenated) to Binance symbols
                bool migrated = false;
                for (int i = 0; i < list.Count; i++)
                {
                    var mapped = MapCoinGeckoId(list[i].Id);
                    if (mapped != null)
                    {
                        list[i] = list[i] with { Id = mapped, Symbol = mapped };
                        migrated = true;
                    }
                }
                if (migrated) await SaveAsync(list).ConfigureAwait(false);
                return list;
            }
            return GetDefaults();
        }
        catch
        {
            return GetDefaults();
        }
    }

    private static string? MapCoinGeckoId(string id) => id switch
    {
        "bitcoin"     => "BTC",
        "ethereum"    => "ETH",
        "solana"      => "SOL",
        "cardano"     => "ADA",
        "ripple"      => "XRP",
        "dogecoin"    => "DOGE",
        "litecoin"    => "LTC",
        "binancecoin" => "BNB",
        "polkadot"    => "DOT",
        "avalanche-2" => "AVAX",
        "chainlink"   => "LINK",
        "uniswap"     => "UNI",
        "stellar"     => "XLM",
        "monero"      => "XMR",
        // already a Binance symbol — no migration needed
        _ when !id.Contains('-') && id == id.ToUpperInvariant() => null,
        _ => null
    };

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
        new("BTC", "BTC", "Bitcoin"),
        new("ETH", "ETH", "Ethereum"),
        new("SOL", "SOL", "Solana"),
    ];
}
