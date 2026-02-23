using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoPriceWidget.Services;

public record CoinInfo(string Id, string Symbol, string Name);

public class CryptoPriceService
{
    // Spacing between individual coin requests to stay within the free-tier rate limit
    private static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(1500);

    private static readonly HttpClient _client = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "CryptoPriceWidget/1.0" } }
    };

    // Fetch prices — one request per coin ID with rate-limit awareness
    public async Task<Dictionary<string, decimal>?> GetPricesAsync(IEnumerable<string> coinIds)
    {
        var result = new Dictionary<string, decimal>();
        bool anyError = false;
        bool first = true;

        foreach (var coinId in coinIds)
        {
            // Throttle: wait between requests (skip before the very first one)
            if (!first)
                await Task.Delay(RequestDelay).ConfigureAwait(false);
            first = false;

            decimal? price = await FetchPriceWithRetryAsync(coinId).ConfigureAwait(false);
            if (price.HasValue)
                result[coinId] = price.Value;
            else
                anyError = true;
        }

        // Return null only if every request failed
        return result.Count == 0 && anyError ? null : result;
    }

    private async Task<decimal?> FetchPriceWithRetryAsync(string coinId)
    {
        try
        {
            var url = $"https://api.coingecko.com/api/v3/simple/price?ids={coinId}&vs_currencies=usd";
            using var response = await _client.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Console.Error.WriteLine($"[CryptoPriceService] 429 for {coinId} — skipping, will retry next cycle.");
                return null;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty(coinId, out var coinEl) &&
                coinEl.TryGetProperty("usd", out var priceEl))
                return priceEl.GetDecimal();

            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CryptoPriceService] Failed to fetch price for {coinId}: {ex.Message}");
            return null;
        }
    }

    // Search CoinGecko for a coin by name or symbol, returns best match
    public async Task<CoinInfo?> SearchCoinAsync(string query)
    {
        try
        {
            var url = $"https://api.coingecko.com/api/v3/search?query={Uri.EscapeDataString(query)}";
            var json = await _client.GetStringAsync(url).ConfigureAwait(false);
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
