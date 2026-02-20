using Shiny.BluetoothLE;

namespace MotionLink.Services;

public interface IBleService
{
    IPeripheral ConnectedPeripheral {get;}
    IObservable<IPeripheral> ScanForDevices(string serviceUuid);
    Task ConnectAsync(IPeripheral peripheral);
    void DisconnectDevice();
}