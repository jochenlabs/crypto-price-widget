using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    private readonly CryptoPriceService _priceService = new();
    private readonly CoinStorageService _storage = new();
    private readonly SettingsService _settingsService = new();
    private readonly Timer _timer;
    private readonly DispatcherTimer _clockTimer;
    private bool _initializing = true;

    private bool _isVertical;
    public bool IsVertical
    {
        get => _isVertical;
        set
        {
            if (_isVertical == value) return;
            _isVertical = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    private bool _isTopmost = true;
    public bool IsTopmost
    {
        get => _isTopmost;
        set
        {
            if (_isTopmost == value) return;
            _isTopmost = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    private void SaveSettings()
    {
        _ = _settingsService.SaveAsync(new AppSettings(IsVertical: _isVertical, IsTopmost: _isTopmost)).ConfigureAwait(false);
    }

    public ObservableCollection<CoinTileViewModel> Coins { get; } = new();


    public MainViewModel()
    {
        Coins.CollectionChanged += OnCoinsCollectionChanged;

        _ = InitializeAsync();

        _timer = new Timer(_ => _ = RefreshAsync(), null,
                           TimeSpan.FromMinutes(5),
                           TimeSpan.FromMinutes(5));

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _clockTimer.Tick += (_, _) => { foreach (var c in Coins) c.NotifyTimeUpdated(); };
        _clockTimer.Start();
    }

    private async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadAsync().ConfigureAwait(false);
        var saved    = await _storage.LoadAsync().ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _isVertical = settings.IsVertical;
            OnPropertyChanged(nameof(IsVertical));
            _isTopmost = settings.IsTopmost;
            OnPropertyChanged(nameof(IsTopmost));

            foreach (var s in saved)
                AddCoinToCollection(s.Id, s.Symbol, s.Name, s.LastPrice, s.LastUpdatedAt);

            _initializing = false;
        });

        await RefreshAsync().ConfigureAwait(false);
    }

    private void AddCoinToCollection(string id, string symbol, string name,
                                       decimal? cachedPrice = null, DateTime? cachedAt = null)
    {
        var tile = new CoinTileViewModel(id, symbol, name)
        {
            RemoveRequested = RemoveCoin
        };
        if (cachedPrice.HasValue && cachedAt.HasValue)
            tile.LoadCachedPrice(cachedPrice.Value, cachedAt.Value);
        Coins.Add(tile);
    }

    private void OnCoinsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_initializing) return;
        SaveCoinsToFile();
    }

    private void SaveCoinsToFile()
    {
        var snapshot = Coins
            .Select(c => new SavedCoin(c.CoinId, c.Symbol, c.Name, c.PriceRaw, c.PriceUpdatedAt))
            .ToList();
        _ = _storage.SaveAsync(snapshot);
    }

    // Returns null on success, or an error message.
    public async Task<string?> TryAddCoinAsync(string query)
    {
        var info = await _priceService.SearchCoinAsync(query.Trim()).ConfigureAwait(false);
        if (info is null)
            return $"No coin found for \"{query}\".";

        if (Coins.Any(c => c.CoinId == info.Id))
            return $"{info.Name} is already in the list.";

        await Dispatcher.UIThread.InvokeAsync(() =>
            AddCoinToCollection(info.Id, info.Symbol, info.Name));

        await RefreshAsync().ConfigureAwait(false);
        return null;
    }

    public void RemoveCoin(CoinTileViewModel coin)
    {
        Dispatcher.UIThread.Post(() => Coins.Remove(coin));
    }

    public void MoveUp(CoinTileViewModel coin)
    {
        var idx = Coins.IndexOf(coin);
        if (idx > 0)
            Coins.Move(idx, idx - 1);
    }

    public void MoveDown(CoinTileViewModel coin)
    {
        var idx = Coins.IndexOf(coin);
        if (idx >= 0 && idx < Coins.Count - 1)
            Coins.Move(idx, idx + 1);
    }

    public async Task RefreshAsync()
    {
        if (!Coins.Any()) return;

        var ids = Coins.Select(c => c.CoinId).ToList();
        var prices = await _priceService.GetPricesAsync(ids).ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var coin in Coins)
            {
                if (prices is not null && prices.TryGetValue(coin.CoinId, out var price))
                    coin.SetPrice(price);
                else
                    coin.MarkSyncFailed();
            }
        });

        // Persist updated prices so they're shown immediately on next launch
        SaveCoinsToFile();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        _timer.Dispose();
        _clockTimer.Stop();
    }
}
