using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotionLink.Services;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using MotionLink.Views;

namespace MotionLink.ViewModels;

public partial class PeripheralConnectViewModel : BaseViewModel
{

    [ObservableProperty]
    private IBleService _bleService;
    public IDisposable? scanSub;

    [ObservableProperty]
    private ObservableCollection<IPeripheral> _devices = new();

    [ObservableProperty]
    private bool _isScanning;

    private readonly INavigationService _navigationService;

    private string TargetServiceUuid { get; } = "19B10000-E8F2-537E-4F6C-D104768A1214";

    public PeripheralConnectViewModel(IBleService bleService, INavigationService navigationService)
    {
        _bleService = bleService;
        _navigationService = navigationService;
    }

    
    [RelayCommand]
    void StartScan()
    {
        if (IsScanning)
            return;

        Devices.Clear();
        IsScanning = true;

        scanSub = BleService
            .ScanForDevices(TargetServiceUuid)
            .Subscribe(device =>
            {
                if (!Devices.Contains(device))
                    Devices.Add(device);
            });

        // scanSub?.Dispose();
        // scanSub = null;
        // IsScanning = false;
    }

    [RelayCommand]
    void StopScan()
    {
        scanSub?.Dispose();
        scanSub = null;
        IsScanning = false;
    }

    [RelayCommand]
    async Task ConnectAsync(IPeripheral peripheral)
    {
        if (peripheral == null) return;

        try 
        {
            await BleService.ConnectAsync(peripheral);
            StopScan();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to device with uuid: {peripheral.Uuid}, {ex}");
        }
    }

    [RelayCommand]
    void Disconnect()
    {
        BleService.DisconnectDevice();
    }

    [RelayCommand]
    async Task NavigateToDashboard()
    {
        await _navigationService.NavigateToDashboard();
    }


}