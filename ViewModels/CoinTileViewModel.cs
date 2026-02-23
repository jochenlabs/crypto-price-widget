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
        set { if (_price != value) { _price = value; OnPropertyChanged(); } }
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
