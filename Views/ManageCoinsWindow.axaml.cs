using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CryptoPriceWidget.Services;
using CryptoPriceWidget.ViewModels;

namespace CryptoPriceWidget.Views;

public partial class ManageCoinsWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _busy;

    // Design-time constructor
    public ManageCoinsWindow() : this(new MainViewModel()) { }

    public ManageCoinsWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        FilePathRun.Text = CoinStorageService.FilePath;
        SearchBox.Focus();
    }

    // ── Move Up ───────────────────────────────────────────────────────
    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CoinTileViewModel coin })
            _vm.MoveUp(coin);
    }

    // ── Move Down ─────────────────────────────────────────────────────
    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CoinTileViewModel coin })
            _vm.MoveDown(coin);
    }

    // ── Remove ────────────────────────────────────────────────────────
    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CoinTileViewModel coin })
            _vm.RemoveCoin(coin);
    }

    // ── Add ───────────────────────────────────────────────────────────
    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) _ = DoAddAsync();
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
        => _ = DoAddAsync();

    private async System.Threading.Tasks.Task DoAddAsync()
    {
        if (_busy) return;
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        _busy = true;
        AddButton.IsEnabled = false;
        AddButton.Content = "Searching…";
        StatusText.IsVisible = false;

        var error = await _vm.TryAddCoinAsync(query);

        if (error is null)
        {
            // Success — clear the input, ready for another add
            SearchBox.Text = string.Empty;
            AddButton.Content = "Add";
            AddButton.IsEnabled = true;
            _busy = false;
            return;
        }

        // Show error
        StatusText.Text = error;
        StatusText.IsVisible = true;
        AddButton.Content = "Add";
        AddButton.IsEnabled = true;
        _busy = false;
    }
}
