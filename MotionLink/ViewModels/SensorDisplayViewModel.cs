using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotionLink.Services;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using MotionLink.Views;
using Microsoft.Extensions.Logging;
using MotionLink.Repositories;

namespace MotionLink.ViewModels;

public partial class SensorDisplayViewModel : BaseViewModel
{
    private readonly ILogger<SensorDisplayViewModel> _logger;

    [ObservableProperty]
    private IBleService _bleService;
    private IMotionLinkRepository _repo;
    private readonly INavigationService _navigationService;
    private int? _currentSessionId;

    [ObservableProperty]
    private bool _isCapturing = false;
    public SensorDisplayViewModel(ILogger<SensorDisplayViewModel> logger, IBleService bleService, IMotionLinkRepository repo, INavigationService navigationService)
    {
        _logger = logger;
        _bleService = bleService;
        _repo = repo;
        _navigationService = navigationService;
    }

    public async Task InitializeAsync()
    {
        try 
        {
            var result = await BleService.ConnectedPeripheral.ReadCharacteristicAsync(
                "19B10000-E8F2-537E-4F6C-D104768A1214", 
                "FB7A8DB8-DE34-4C6F-9F98-D1612EE35441");

            if (result?.Data != null && result.Data.Length > 0)
            {
                IsCapturing = result.Data[0] == 0x01;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Initial Read Failed: {ex.Message}");
            IsCapturing = false;
        }
    }

    [RelayCommand]
    async Task NavigateToConnect()
    {
        await _navigationService.NavigateToConnect();
    }


    [RelayCommand]
    async Task CaptureData()
    {
        try 
        {
            BleCharacteristicResult result = await BleService.ConnectedPeripheral.WriteCharacteristicAsync("19B10000-E8F2-537E-4F6C-D104768A1214", "FB7A8DB8-DE34-4C6F-9F98-D1612EE35441", [0x01]);
            IsCapturing = true;
            _currentSessionId = await _repo.CreateSessionAsync(DateTime.Now,default);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError("Arduino didn't respond to the start command, but we might still get notifications.");
        }
    }

    [RelayCommand]
    async Task PauseData()
    {
        try 
        {
            BleCharacteristicResult result = await BleService.ConnectedPeripheral.WriteCharacteristicAsync("19B10000-E8F2-537E-4F6C-D104768A1214", "FB7A8DB8-DE34-4C6F-9F98-D1612EE35441", [0x00]);
            IsCapturing = false;

            if (_currentSessionId != null)
            {
                await _repo.UpdateSessionAsync((int) _currentSessionId, $"session-{DateTime.Now}", DateTime.Now, default);
            }

            _currentSessionId = null;

        }
        catch (TimeoutException ex)
        {
            _logger.LogError("Arduino didn't respond to the stop command, but we might still get notifications.");
        }
    }

    
}