using System.Collections.ObjectModel;
using LiveChartsCore;
using MotionLink.Models;
using Shiny.BluetoothLE;

namespace MotionLink.Services;

public interface IBleService : System.ComponentModel.INotifyPropertyChanged
{
    ISwingProcessingService SwingProcessingService {get;}
    ILiveChartService LiveChartService {get;}
    IPeripheral ConnectedPeripheral {get;}
    bool IsConnected { get; }
    IObservable<IPeripheral> ScanForDevices(string serviceUuid);
    Task ConnectAsync(IPeripheral peripheral);
    void DisconnectDevice();
}