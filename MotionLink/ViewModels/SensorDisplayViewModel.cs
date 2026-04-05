using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotionLink.Services;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using MotionLink.Views;
using Microsoft.Extensions.Logging;

namespace MotionLink.ViewModels;

public partial class SensorDisplayViewModel : BaseViewModel
{

    private readonly ILogger<SensorDisplayViewModel> _logger;

    [ObservableProperty]
    private IBleService _bleService;

    [ObservableProperty]
    private bool _isCapturing = false;
    public SensorDisplayViewModel(ILogger<SensorDisplayViewModel> logger, IBleService bleService)
    {
        _logger = logger;
        _bleService = bleService;
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
        await Shell.Current.GoToAsync($"///{nameof(PeripheralConnectView)}");
    }


    [RelayCommand]
    async Task CaptureData()
    {
        try 
        {
            BleCharacteristicResult result = await BleService.ConnectedPeripheral.WriteCharacteristicAsync("19B10000-E8F2-537E-4F6C-D104768A1214", "FB7A8DB8-DE34-4C6F-9F98-D1612EE35441", [0x01]);
            IsCapturing = true;
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
        }
        catch (TimeoutException ex)
        {
            _logger.LogError("Arduino didn't respond to the start command, but we might still get notifications.");
        }
    }

    
}