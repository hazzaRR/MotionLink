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
    private readonly SimpleMovingAverage _filterAx = new(5);
    private readonly SimpleMovingAverage _filterAy = new(5);
    private readonly SimpleMovingAverage _filterAz = new(5);

    [ObservableProperty] 
    private double _peakRotation; // Proxy for swing speed
    [ObservableProperty]
    private double _peakGForce;

    [ObservableProperty]
    private IPeripheral? _connectedPeripheral;
    public bool IsConnected => ConnectedPeripheral != null;

    [ObservableProperty]
    private ImuPacket _lastValue;

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
        
        AccelSeries =
        [

            new LineSeries<double> { Values = AccX, Name = "Acc X", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = AccY, Name = "Acc Y", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = AccZ, Name = "Acc Z", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 4 }},

        ];

        GyroSeries =
        [
            new LineSeries<double> { Values = GyroX, Name = "Gyro X", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = GyroY, Name = "Gyro Y", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = GyroZ, Name = "Gyro Z", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 4 }}
        ];

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

            double rawAx = BitConverter.ToSingle(data.Data, 0);
            double rawAy = BitConverter.ToSingle(data.Data, 4);
            double rawAz = BitConverter.ToSingle(data.Data, 8);
            double rawGx = BitConverter.ToSingle(data.Data, 12);
            double rawGy = BitConverter.ToSingle(data.Data, 16);
            double rawGz = BitConverter.ToSingle(data.Data, 20);
                    
            double cleanAx = _filterAx.Compute(rawAx);
            double cleanAy = _filterAy.Compute(rawAy);
            double cleanAz = _filterAz.Compute(rawAz);

            double currentG = Math.Sqrt(cleanAx * cleanAx + cleanAy * cleanAy + cleanAz * cleanAz);
            double currentRot = Math.Sqrt(rawGx * rawGx + rawGy * rawGy + rawGz * rawGz);

            // // Convert Degrees/Second to Radians/Second
            // double radiansPerSecond = currentRot * (Math.PI / 180);

            // // Calculate Linear Speed (Meters per Second)
            // double metersPerSecond = radiansPerSecond * 1.14;

            // // Convert to MPH
            // double mph = metersPerSecond * 2.237;

            // // Update a new property
            // PeakMph = mph;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (currentG > PeakGForce) PeakGForce = currentG;
                if (currentRot > PeakRotation) PeakRotation = currentRot;
                // if (currentRot > PeakRotation) PeakRotation = currentRot;

                LastValue = new ImuPacket { TimeStamp = DateTime.Now, Ax = cleanAx, Ay = cleanAy, Az = cleanAz, Gx = rawGx, Gy = rawGy, Gz = rawGz };

                lock (Sync)
                {
                    AddPoint(AccX, cleanAx);
                    AddPoint(AccY, cleanAy);
                    AddPoint(AccZ, cleanAz);
                    AddPoint(GyroX, rawGx);
                    AddPoint(GyroY, rawGy);
                    AddPoint(GyroZ, rawGz);
                }
            });

        });
        
    }

    private void AddPoint(ObservableCollection<double> list, double value)
    {
        list.Add(value);

        if (list.Count > 100)
            list.RemoveAt(0);
    }

    public void DisconnectDevice()
    {
        ConnectedPeripheral?.CancelConnection();
        ConnectedPeripheral = null;
    }

}