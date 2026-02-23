using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoPriceWidget.Services;

public record CoinInfo(string Id, string Symbol, string Name);

public class CryptoPriceService
{
    // Spacing between individual coin requests to stay within the free-tier rate limit
    private static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(1500);
    // How long to wait before retrying after a 429 response
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

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
                await Task.Delay(RequestDelay);
            first = false;

            decimal? price = await FetchPriceWithRetryAsync(coinId);
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
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var url = $"https://api.coingecko.com/api/v3/simple/price?ids={coinId}&vs_currencies=usd";
                using var response = await _client.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    // Respect Retry-After header if present, otherwise use default
                    int retryAfter = RetryDelay.Seconds;
                    if (response.Headers.TryGetValues("Retry-After", out var values) &&
                        int.TryParse(values.FirstOrDefault(), out var headerVal))
                        retryAfter = headerVal;

                    Console.Error.WriteLine($"[CryptoPriceService] 429 for {coinId} — waiting {retryAfter}s before retry.");
                    await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                    continue; // retry
                }

                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty(coinId, out var coinEl) &&
                    coinEl.TryGetProperty("usd", out var priceEl))
                    return priceEl.GetDecimal();

                return null;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CryptoPriceService] Failed to fetch price for {coinId} (attempt {attempt + 1}): {ex.Message}");
            }
        }

        return null;
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
