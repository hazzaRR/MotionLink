using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Shiny.BluetoothLE;


namespace MotionLink.Services;

public partial class BleService : ObservableObject, IBleService
{
    private readonly IBleManager _bleManager;

    [ObservableProperty]
    private IPeripheral? _connectedPeripheral;
    public bool IsConnected => ConnectedPeripheral != null;
    public BleService(IBleManager bleManager)
    {
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
            }
        });
    }
    public void DisconnectDevice()
    {
        ConnectedPeripheral?.CancelConnection();
        ConnectedPeripheral = null;
    }

}