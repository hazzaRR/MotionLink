using MotionLink.Models;
using Shiny.BluetoothLE;

namespace MotionLink.Services;

public interface IBleService
{
    ROMPacket LastValue {get;}
    IPeripheral ConnectedPeripheral {get;}
    IObservable<IPeripheral> ScanForDevices(string serviceUuid);
    Task ConnectAsync(IPeripheral peripheral);
    void DisconnectDevice();
}