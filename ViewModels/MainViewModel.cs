using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CryptoPriceWidget.Services;

namespace CryptoPriceWidget.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly CryptoPriceService _service = new();
    private readonly Timer _timer;

    public ObservableCollection<CoinTileViewModel> Coins { get; } = new();

    private string _lastUpdated = "–";
    private string _statusColor = "#888888";

    public string LastUpdated
    {
        get => _lastUpdated;
        private set { _lastUpdated = value; OnPropertyChanged(); }
    }

    public string StatusColor
    {
        get => _statusColor;
        private set { _statusColor = value; OnPropertyChanged(); }
    }

    public MainViewModel()
    {
        AddDefaultCoin("bitcoin",  "BTC", "Bitcoin");
        AddDefaultCoin("ethereum", "ETH", "Ethereum");

        _ = RefreshAsync();
        _timer = new Timer(_ => _ = RefreshAsync(), null,
                           TimeSpan.FromSeconds(30),
                           TimeSpan.FromSeconds(30));
    }

    private void AddDefaultCoin(string id, string symbol, string name)
    {
        var tile = new CoinTileViewModel(id, symbol, name)
        {
            RemoveRequested = RemoveCoin
        };
        Coins.Add(tile);
    }

    // Returns null on success, or an error string to show the user
    public async Task<string?> TryAddCoinAsync(string query)
    {
        // Search CoinGecko
        var info = await _service.SearchCoinAsync(query.Trim());
        if (info is null)
            return $"No coin found for \"{query}\".";

        // Already in the list?
        if (Coins.Any(c => c.CoinId == info.Id))
            return $"{info.Name} is already in the toolbar.";

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var tile = new CoinTileViewModel(info.Id, info.Symbol, info.Name)
            {
                RemoveRequested = RemoveCoin
            };
            Coins.Add(tile);
        });

        // Refresh prices immediately to populate the new tile
        await RefreshAsync();
        return null;
    }

    public void RemoveCoin(CoinTileViewModel coin)
    {
        Dispatcher.UIThread.Post(() => Coins.Remove(coin));
    }

    public async Task RefreshAsync()
    {
        if (!Coins.Any()) return;

        var ids = Coins.Select(c => c.CoinId).ToList();
        var prices = await _service.GetPricesAsync(ids);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (prices is null)
            {
                StatusColor  = "#E05555";
                LastUpdated  = "Error – retrying…";
                return;
            }

            foreach (var coin in Coins)
            {
                if (prices.TryGetValue(coin.CoinId, out var price))
                    coin.Price = $"${price:N2}";
            }

            LastUpdated = $"Updated {DateTime.Now:HH:mm:ss}";
            StatusColor = "#4CAF50";
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose() => _timer.Dispose();
}
