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
    private static readonly HttpClient _client = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "CryptoPriceWidget/1.0" } }
    };

    // Fetch prices for all coin IDs in one batch request.
    // CoinId is the Binance base asset symbol, e.g. "BTC" → queries BTCUSDT.
    public async Task<Dictionary<string, decimal>?> GetPricesAsync(IEnumerable<string> coinIds)
    {
        var ids = coinIds.ToList();
        if (ids.Count == 0) return new();

        try
        {
            // Build JSON array of symbols: ["BTCUSDT","ETHUSDT"]
            var symbolsJson = "[" + string.Join(",", ids.Select(id => $"\"{id}USDT\"")) + "]";
            var url = $"https://api.binance.com/api/v3/ticker/price?symbols={Uri.EscapeDataString(symbolsJson)}";

            using var response = await _client.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Console.Error.WriteLine("[CryptoPriceService] 429 from Binance — will retry next cycle.");
                return null;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            var result = new Dictionary<string, decimal>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var sym    = item.GetProperty("symbol").GetString()!; // e.g. "BTCUSDT"
                var baseId = sym.EndsWith("USDT") ? sym[..^4] : sym;  // strip USDT → "BTC"
                if (decimal.TryParse(item.GetProperty("price").GetString(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var price))
                    result[baseId] = price;
            }
            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CryptoPriceService] GetPricesAsync failed: {ex.Message}");
            return null;
        }
    }

    // Search Binance for a coin. Tries exact USDT pair first, then fuzzy via exchangeInfo.
    public async Task<CoinInfo?> SearchCoinAsync(string query)
    {
        var sym = query.Trim().ToUpperInvariant();
        // Strip USDT suffix if user typed the full pair
        if (sym.EndsWith("USDT")) sym = sym[..^4];

        // Fast path: verify the USDT pair exists
        try
        {
            var url = $"https://api.binance.com/api/v3/ticker/price?symbol={sym}USDT";
            using var response = await _client.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return new CoinInfo(sym, sym, sym);
        }
        catch { }

        // Fuzzy path: scan USDT pairs from exchangeInfo
        try
        {
            var url  = "https://api.binance.com/api/v3/exchangeInfo";
            var json = await _client.GetStringAsync(url).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            foreach (var s in doc.RootElement.GetProperty("symbols").EnumerateArray())
            {
                if (s.GetProperty("quoteAsset").GetString() != "USDT") continue;
                var status = s.GetProperty("status").GetString();
                if (status != "TRADING") continue;
                var baseAsset = s.GetProperty("baseAsset").GetString()!;
                if (baseAsset.Contains(sym))
                    return new CoinInfo(baseAsset, baseAsset, baseAsset);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CryptoPriceService] SearchCoinAsync failed: {ex.Message}");
        }

        return null;
    }
}
