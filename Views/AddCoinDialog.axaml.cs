using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CryptoPriceWidget.ViewModels;

namespace CryptoPriceWidget.Views;

public partial class AddCoinDialog : Window
{
    private readonly MainViewModel _vm;
    private bool _busy;

    // Design-time constructor
    public AddCoinDialog() : this(new MainViewModel()) { }

    public AddCoinDialog(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        SearchBox.Focus();
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) _ = DoAddAsync();
        if (e.Key == Key.Escape) Close();
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
        => _ = DoAddAsync();

    private void Cancel_Click(object? sender, RoutedEventArgs e)
        => Close();

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
            // Success — close the dialog
            Close();
            return;
        }

        // Show error and let user try again
        StatusText.Text = error;
        StatusText.IsVisible = true;
        AddButton.Content = "Add";
        AddButton.IsEnabled = true;
        _busy = false;
    }
}
