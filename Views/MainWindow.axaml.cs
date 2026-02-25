using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CryptoPriceWidget.ViewModels;

namespace CryptoPriceWidget.Views;

public partial class MainWindow : Window
{
    private bool _isPinned = true;
    private MainViewModel _vm = null!;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;

        // Reposition whenever coins are added/removed (window width changes)
        _vm.Coins.CollectionChanged += OnCoinsChanged;
    }

    private void OnCoinsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Delay until after layout so Bounds.Width is updated
        Dispatcher.UIThread.Post(PositionAtCenterTop, DispatcherPriority.Render);
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        Dispatcher.UIThread.Post(PositionAtCenterTop, DispatcherPriority.Render);
        ApplyPinVisuals();
    }

    private void ApplyPinVisuals()
    {
        if (PinButton is null) return;
        if (_isPinned)
        {
            PinButton.Background      = new SolidColorBrush(Color.Parse("#5f5f5f"));
            PinButton.Foreground      = new SolidColorBrush(Colors.White);
            PinButton.BorderBrush     = new SolidColorBrush(Color.Parse("#5f5f5f"));
            PinButton.BorderThickness = new Thickness(1);
            ToolTip.SetTip(PinButton, "Pinned — click to unpin");
        }
        else
        {
            PinButton.Background      = new SolidColorBrush(Colors.Transparent);
            PinButton.Foreground      = new SolidColorBrush(Color.Parse("#555555"));
            PinButton.BorderBrush     = new SolidColorBrush(Colors.Transparent);
            PinButton.BorderThickness = new Thickness(0);
            ToolTip.SetTip(PinButton, "Always on top");
        }
    }

    private void PositionAtCenterTop()
    {
        var screen = Screens.Primary;
        if (screen is null) return;

        var workArea = screen.WorkingArea;
        var scale    = screen.Scaling;

        // Use actual rendered width (Bounds.Width) since SizeToContent="Width" makes Width=NaN
        double logicalWidth = Bounds.Width > 0 ? Bounds.Width : MinWidth;
        double physX = workArea.X + (workArea.Width - logicalWidth * scale) / 2.0;
        double physY = workArea.Y + (int)(8 * scale);

        Position = new PixelPoint((int)physX, (int)physY);
    }

    private void PinButton_Click(object? sender, RoutedEventArgs e)
    {
        _isPinned = !_isPinned;
        Topmost = _isPinned;
        ApplyPinVisuals();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        RefreshButton.IsEnabled = false;
        _ = _vm.RefreshAsync().ContinueWith(_ =>
            Dispatcher.UIThread.Post(() => RefreshButton.IsEnabled = true));
    }

    private void ManageCoins_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new ManageCoinsWindow(_vm) { Topmost = true };
        dialog.ShowDialog(this);
    }

    private void DragRegion_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
}
