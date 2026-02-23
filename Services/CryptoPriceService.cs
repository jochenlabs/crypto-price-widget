using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoPriceWidget.Services;

public record CoinInfo(string Id, string Symbol, string Name);

public class CryptoPriceService
{
    private static readonly HttpClient _client = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "CryptoPriceWidget/1.0" } }
    };

    // Fetch prices for any set of CoinGecko IDs
    public async Task<Dictionary<string, decimal>?> GetPricesAsync(IEnumerable<string> coinIds)
    {
        try
        {
            var ids = string.Join(",", coinIds);
            var url = $"https://api.coingecko.com/api/v3/simple/price?ids={ids}&vs_currencies=usd";
            var json = await _client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            var result = new Dictionary<string, decimal>();
            foreach (var coinId in coinIds)
            {
                if (doc.RootElement.TryGetProperty(coinId, out var coinEl) &&
                    coinEl.TryGetProperty("usd", out var priceEl))
                {
                    result[coinId] = priceEl.GetDecimal();
                }
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    // Search CoinGecko for a coin by name or symbol, returns best match
    public async Task<CoinInfo?> SearchCoinAsync(string query)
    {
        try
        {
            var url = $"https://api.coingecko.com/api/v3/search?query={Uri.EscapeDataString(query)}";
            var json = await _client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            var coins = doc.RootElement.GetProperty("coins");
            if (coins.GetArrayLength() == 0) return null;

            var first = coins[0];
            return new CoinInfo(
                first.GetProperty("id").GetString()!,
                first.GetProperty("symbol").GetString()!,
                first.GetProperty("name").GetString()!
            );
        }
        catch
        {
            return null;
        }
    }
}
