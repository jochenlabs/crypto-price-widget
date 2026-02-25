using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using CryptoPriceWidget.ViewModels;

namespace CryptoPriceWidget.Views;

public partial class MainWindow : Window
{
    private MainViewModel _vm = null!;
    private bool _initialPositionApplied = false;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;

        // Reposition whenever coins are added/removed (window width changes)
        _vm.Coins.CollectionChanged += OnCoinsChanged;
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        PositionChanged += OnPositionChanged;
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (_initialPositionApplied)
            _vm.SaveWindowPosition(e.Point.X, e.Point.Y);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsVertical))
            Dispatcher.UIThread.Post(() => ApplyOrientation(_vm.IsVertical));
        if (e.PropertyName == nameof(MainViewModel.IsTopmost))
            Dispatcher.UIThread.Post(() => Topmost = _vm.IsTopmost);
    }

    private void OnCoinsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Delay until after layout so Bounds.Width is updated
        Dispatcher.UIThread.Post(PositionAtCenterTop, DispatcherPriority.Render);
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        Topmost = _vm.IsTopmost;
        ApplyOrientation(_vm.IsVertical);
    }

    private void ApplyOrientation(bool isVertical)
    {
        if (isVertical)
        {
            SizeToContent = SizeToContent.WidthAndHeight;
            Width     = double.NaN;
            MinWidth  = 100;
            MaxWidth  = double.PositiveInfinity;
            MinHeight = 80;
            MaxHeight = double.PositiveInfinity;
            DockPanel.SetDock(ButtonsPanel, Dock.Bottom);
            ButtonsPanel.HorizontalAlignment = HorizontalAlignment.Center;
            CoinItemsControl.ItemsPanel = new FuncTemplate<Panel?>(
                () => new StackPanel { Orientation = Orientation.Vertical });
            CoinItemsControl.ItemTemplate = (IDataTemplate)this.Resources["VerticalTileTemplate"]!;
        }
        else
        {
            SizeToContent = SizeToContent.Width;
            Width     = double.NaN;
            MinWidth  = 400;
            MaxWidth  = double.PositiveInfinity;
            Height    = 64;
            MinHeight = 64;
            MaxHeight = 64;
            DockPanel.SetDock(ButtonsPanel, Dock.Right);
            ButtonsPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            CoinItemsControl.ItemsPanel = new FuncTemplate<Panel?>(
                () => new StackPanel { Orientation = Orientation.Horizontal });
            CoinItemsControl.ItemTemplate = (IDataTemplate)this.Resources["HorizontalTileTemplate"]!;
        }
        Dispatcher.UIThread.Post(PositionAtCenterTop, DispatcherPriority.Render);
    }

    private void PositionAtCenterTop()
    {
        if (_vm.WindowX.HasValue && _vm.WindowY.HasValue)
        {
            Position = new PixelPoint(_vm.WindowX.Value, _vm.WindowY.Value);
            _initialPositionApplied = true;
            return;
        }

        var screen = Screens.Primary;
        if (screen is null) return;

        var workArea = screen.WorkingArea;
        var scale    = screen.Scaling;

        double logicalWidth = Bounds.Width > 0 ? Bounds.Width : MinWidth;
        double physX = workArea.X + (workArea.Width - logicalWidth * scale) / 2.0;
        double physY = workArea.Y + (int)(8 * scale);

        Position = new PixelPoint((int)physX, (int)physY);
        _initialPositionApplied = true;
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
        var dialog = new ManageWindow(_vm) { Topmost = true };
        dialog.ShowDialog(this);
    }

    private void DragRegion_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
}
