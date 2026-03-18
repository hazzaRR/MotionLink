using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using MotionLink.Models;
using Shiny;
using Shiny.BluetoothLE;


namespace MotionLink.Services;

public partial class BleService : ObservableObject, IBleService
{
    private readonly ILogger<BleService> _logger;
    private readonly IBleManager _bleManager;

    [ObservableProperty]
    private ROMPacket _lastValue;
    private string _lastValueArray;

    [ObservableProperty]
    private IPeripheral? _connectedPeripheral;
    public bool IsConnected => ConnectedPeripheral != null;
    public BleService(ILogger<BleService> logger, IBleManager bleManager)
    {
        _logger = logger;
        _bleManager = bleManager;
    }

    public IObservable<IPeripheral> ScanForDevices(string serviceUuid)
    {
        return _bleManager
            .Scan(new ScanConfig
            {
                ServiceUuids = [serviceUuid]
            }).Select(x => x.Peripheral);
    }

    public async Task ConnectAsync(IPeripheral peripheral)
    {
        
        await peripheral.ConnectAsync();
        ConnectedPeripheral = peripheral;

        OnPropertyChanged(nameof(IsConnected));
        
        peripheral.WhenStatusChanged().Subscribe(status => {
            if (status == ConnectionState.Disconnected)
            {
                ConnectedPeripheral = null;
                 OnPropertyChanged(nameof(IsConnected));
            }
        });

        await ConnectedPeripheral.TryRequestMtuAsync(32);

        ConnectedPeripheral.NotifyCharacteristic("19B10000-E8F2-537E-4F6C-D104768A1214", "36151377-DDD7-4C2F-9207-0724C52C3CCC")
        .Subscribe(data =>
        {
            _logger.LogInformation($"{data.Data.Length}");

            _logger.LogInformation(BitConverter.ToString(data.Data));
            
           ROMPacket packet = new ROMPacket
            {
                Ax = BitConverter.ToSingle(data.Data, 0),
                Ay = BitConverter.ToSingle(data.Data, 4),
                Az = BitConverter.ToSingle(data.Data, 8),
                Gx = BitConverter.ToSingle(data.Data, 12),
                Gy = BitConverter.ToSingle(data.Data, 16),
                Gz = BitConverter.ToSingle(data.Data, 20)
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LastValue = packet;
            });
        });
        
    }
    public void DisconnectDevice()
    {
        ConnectedPeripheral?.CancelConnection();
        ConnectedPeripheral = null;
    }

}