using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotionLink.Services;
using Shiny.BluetoothLE;
using System.Reactive.Linq;

namespace MotionLink.ViewModels;

public partial class PeripheralConnectViewModel : BaseViewModel
{

    public IBleService BleService;
    public IDisposable? scanSub;

    [ObservableProperty]
    private ObservableCollection<IPeripheral> _devices = new();

    [ObservableProperty]
    private bool _isScanning;

    private string TargetServiceUuid { get; } = "19B10000-E8F2-537E-4F6C-D104768A1214";

    public PeripheralConnectViewModel(IBleService bleService)
    {
        BleService = bleService;
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

        Console.WriteLine(Devices);
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
            await peripheral.ConnectAsync();
        }
        catch (Exception ex)
        {
        }
    }

    

    
}