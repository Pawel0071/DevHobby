using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using RPG.DesktopClient.Avalonia.Services;
using RPG.DesktopClient.Avalonia.ViewModels;

namespace RPG.DesktopClient.Avalonia;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var clientService = new GameClientService(configuration);
        _viewModel = new MainWindowViewModel(clientService);
        DataContext = _viewModel;

        Opened += OnOpened;
        Closed += OnClosed;
        Deactivated += OnDeactivated;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        try
        {
            await _viewModel.InitializeAsync();
            Focus();
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                _viewModel.ReportExternalMessage($"Błąd inicjalizacji: {ex.Message}"));
        }
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        await _viewModel.DisposeAsync();
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (await _viewModel.HandleKeyDownAsync(e.Key))
        {
            e.Handled = true;
        }
    }

    protected override async void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (await _viewModel.HandleKeyUpAsync(e.Key))
        {
            e.Handled = true;
        }
    }

    private async void OnDeactivated(object? sender, EventArgs e)
    {
        await _viewModel.ResetMovementAsync();
    }
}