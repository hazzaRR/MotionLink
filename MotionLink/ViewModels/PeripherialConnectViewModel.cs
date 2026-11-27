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
    private ObservableCollection<DiscoveredPeripheral> _devices = new();

    [ObservableProperty]
    private bool _isScanning;

    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string? _connectionError;

    public partial class DiscoveredPeripheral(IPeripheral peripheral, int rssi) : ObservableObject
    {
        public IPeripheral Peripheral { get; } = peripheral;
        public string? Name => Peripheral.Name;

        [ObservableProperty]
        private int _rssi = rssi;
    }

    public PeripheralConnectViewModel(IBleService bleService, INavigationService navigationService)
    {
        _bleService = bleService;
        _navigationService = navigationService;
        bleService.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IBleService.IsConnected) && bleService.IsConnected)
                MainThread.BeginInvokeOnMainThread(StopScan);
        };
    }

    
    [RelayCommand]
    void StartScan()
    {
        if (IsScanning)
            return;

        ConnectionError = null;
        Devices.Clear();
        IsScanning = true;

        scanSub = BleService.ScanWithSignal()
            .Subscribe(result => MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!IsScanning) return;

                var existing = Devices.FirstOrDefault(device => device.Peripheral.Uuid == result.Peripheral.Uuid);
                if (existing is null)
                    Devices.Add(new DiscoveredPeripheral(result.Peripheral, result.Rssi));
                else
                    existing.Rssi = result.Rssi;
            }), _ => MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionError = "Could not scan. Check Bluetooth and its permissions.";
                StopScan();
            }));
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
            ConnectionError = null;
            StopScan();
            await BleService.ConnectAsync(peripheral);
        }
        catch (Exception ex)
        {
            ConnectionError = "Could not connect to the sensor.";
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