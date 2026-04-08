using System.Collections.ObjectModel;
using LiveChartsCore;
using MotionLink.Models;
using Shiny.BluetoothLE;

namespace MotionLink.Services;

public interface IBleService
{
    ImuPacket LastValue {get;}
    double PeakRotation {get;}
    double PeakGForce { get;}
    List<ImuPacket> SessionData { get; set; }
    ISeries[] AccelSeries { get; }
    ISeries[] GyroSeries { get; }
    object Sync { get; }
    IPeripheral ConnectedPeripheral {get;}
    IObservable<IPeripheral> ScanForDevices(string serviceUuid);
    Task ConnectAsync(IPeripheral peripheral);
    void DisconnectDevice();
}