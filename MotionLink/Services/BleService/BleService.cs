using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using MotionLink.Models;
using Shiny;
using Shiny.BluetoothLE;
using SkiaSharp;


namespace MotionLink.Services;

public partial class BleService : ObservableObject, IBleService
{
    private readonly ILogger<BleService> _logger;
    private readonly IBleManager _bleManager;


    [ObservableProperty]
    private IPeripheral? _connectedPeripheral;
    public bool IsConnected => ConnectedPeripheral != null;

    [ObservableProperty]
    private ROMPacket _lastValue;

    private ObservableCollection<double> AccX { get; } = new();
    private ObservableCollection<double> AccY { get; } = new();
    private ObservableCollection<double> AccZ { get; } = new();
    private ObservableCollection<double> GyroX { get; } = new();
    private ObservableCollection<double> GyroY { get; } = new();
    private ObservableCollection<double> GyroZ { get; } = new();
    public ISeries[] AccelSeries { get; }
    public ISeries[] GyroSeries { get; }
    public object Sync { get; } = new object();
    public BleService(ILogger<BleService> logger, IBleManager bleManager)
    {
        _logger = logger;
        _bleManager = bleManager;
        
        AccelSeries = new ISeries[]
        {

            new LineSeries<double> { Values = AccX, Name = "Acc X", Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = AccY, Name = "Acc Y", Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = AccZ, Name = "Acc Z", Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 4 }},

        };

        GyroSeries = new ISeries[]
        {
            new LineSeries<double> { Values = GyroX, Name = "Gyro X", Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = GyroY, Name = "Gyro Y", Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = GyroZ, Name = "Gyro Z", Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 4 }}
        };

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
                TimeStamp = DateTimeOffset.Now,
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

            lock (Sync)
            {
                AddPoint(AccX, packet.Ax);
                AddPoint(AccY, packet.Ay);
                AddPoint(AccZ, packet.Az);

                AddPoint(GyroX, packet.Gx);
                AddPoint(GyroY, packet.Gy);
                AddPoint(GyroZ, packet.Gz);
            }

        });
        
    }

    private void AddPoint(ObservableCollection<double> list, double value)
    {
        list.Add(value);

        if (list.Count > 200)
            list.RemoveAt(0);
    }

    public void DisconnectDevice()
    {
        ConnectedPeripheral?.CancelConnection();
        ConnectedPeripheral = null;
    }

}