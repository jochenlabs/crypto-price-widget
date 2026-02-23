using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace CryptoPriceWidget.ViewModels;

public class CoinTileViewModel : INotifyPropertyChanged
{
    // ── static lookup tables ──────────────────────────────────────────
    private static readonly Dictionary<string, string> GlyphMap = new()
    {
        ["bitcoin"]        = "₿",
        ["ethereum"]       = "Ξ",
        ["solana"]         = "◎",
        ["cardano"]        = "₳",
        ["ripple"]         = "✕",
        ["dogecoin"]       = "Ð",
        ["litecoin"]       = "Ł",
        ["binancecoin"]    = "BNB",
        ["polkadot"]       = "●",
        ["avalanche-2"]    = "Av",
        ["chainlink"]      = "⬡",
        ["uniswap"]        = "🦄",
        ["stellar"]        = "✦",
        ["monero"]         = "ɱ",
    };

    private static readonly Dictionary<string, string> ColorMap = new()
    {
        ["bitcoin"]        = "#F7931A",
        ["ethereum"]       = "#627EEA",
        ["solana"]         = "#9945FF",
        ["cardano"]        = "#0033AD",
        ["ripple"]         = "#00AAE4",
        ["dogecoin"]       = "#C2A633",
        ["litecoin"]       = "#BFBBBB",
        ["binancecoin"]    = "#F3BA2F",
        ["polkadot"]       = "#E6007A",
        ["avalanche-2"]    = "#E84142",
        ["chainlink"]      = "#2A5ADA",
        ["uniswap"]        = "#FF007A",
        ["stellar"]        = "#7AC4DE",
        ["monero"]         = "#FF6600",
    };

    // ── properties ────────────────────────────────────────────────────
    public string CoinId  { get; }
    public string Symbol  { get; }   // e.g. "BTC"
    public string Name    { get; }   // e.g. "Bitcoin"

    public string Glyph =>
        GlyphMap.TryGetValue(CoinId, out var g) ? g : Symbol[..Math.Min(3, Symbol.Length)];

    public IBrush AccentBrush { get; }

    private string _price = "…";
    public string Price
    {
        get => _price;
        private set { if (_price != value) { _price = value; OnPropertyChanged(); } }
    }

    private decimal? _priceRaw;
    private DateTime? _priceUpdatedAt;

    public decimal? PriceRaw       => _priceRaw;
    public DateTime? PriceUpdatedAt => _priceUpdatedAt;

    public string LastUpdatedText
    {
        get
        {
            if (!_priceUpdatedAt.HasValue) return "\u2013";
            var age = DateTime.Now - _priceUpdatedAt.Value;
            if (age.TotalSeconds < 60)  return "just now";
            if (age.TotalMinutes < 60)  return $"{(int)age.TotalMinutes}m ago";
            return $"{(int)age.TotalHours}h ago";
        }
    }

    public void NotifyTimeUpdated() => OnPropertyChanged(nameof(LastUpdatedText));

    private string _syncDotColor = "#555555";
    public string SyncDotColor
    {
        get => _syncDotColor;
        private set { if (_syncDotColor != value) { _syncDotColor = value; OnPropertyChanged(); } }
    }

    public void MarkSyncFailed() => SyncDotColor = "#E05555";

    /// <summary>Called by the refresh loop with a live price.</summary>
    public void SetPrice(decimal price)
    {
        _priceRaw       = price;
        _priceUpdatedAt = DateTime.Now;
        Price           = $"${price:N2}";
        SyncDotColor    = "#4CAF50";
        OnPropertyChanged(nameof(LastUpdatedText));
    }

    /// <summary>Called on startup to restore the last persisted price.</summary>
    public void LoadCachedPrice(decimal price, DateTime updatedAt)
    {
        _priceRaw       = price;
        _priceUpdatedAt = updatedAt;
        Price           = $"${price:N2}";
        OnPropertyChanged(nameof(LastUpdatedText));
    }

    // Called by the remove (×) button in the UI
    public Action<CoinTileViewModel>? RemoveRequested { get; set; }
    public void RequestRemove() => RemoveRequested?.Invoke(this);

    // ── constructor ───────────────────────────────────────────────────
    public CoinTileViewModel(string coinId, string symbol, string name)
    {
        CoinId = coinId;
        Symbol = symbol.ToUpperInvariant();
        Name   = name;

        var hex = ColorMap.TryGetValue(coinId, out var c) ? c : "#AAAAAA";
        AccentBrush = new SolidColorBrush(Color.Parse(hex));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
