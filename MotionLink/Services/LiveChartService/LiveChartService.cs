using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using MotionLink.Models;
using SkiaSharp;

namespace MotionLink.Services;

public partial class LiveChartService : ObservableObject, ILiveChartService
{
    private readonly ILogger<LiveChartService> _logger;
    private ObservableCollection<double> AccX { get; } = new();
    private ObservableCollection<double> AccY { get; } = new();
    private ObservableCollection<double> AccZ { get; } = new();
    private ObservableCollection<double> GyroX { get; } = new();
    private ObservableCollection<double> GyroY { get; } = new();
    private ObservableCollection<double> GyroZ { get; } = new();
    private ObservableCollection<double> QW { get; } = new();
    private ObservableCollection<double> QX { get; } = new();
    private ObservableCollection<double> QY { get; } = new();
    private ObservableCollection<double> QZ { get; } = new();
    public ISeries[] AccelSeries { get; }
    public ISeries[] GyroSeries { get; }
    public ISeries[] QuaternionSeries { get; }

    [ObservableProperty] 
    private double _peakRotation;
    [ObservableProperty]
    private double _peakGForce;
    public object Sync { get; } = new object();

    public LiveChartService(ILogger<LiveChartService> logger)
    {
        _logger = logger;
        
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

        QuaternionSeries =
        [
            new LineSeries<double> { Values = QW, Name = "W", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = QX, Name = "X", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = QY, Name = "Y", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 4 }},
            new LineSeries<double> { Values = QZ, Name = "Z", GeometrySize = 0, Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 4 }}
        ];

    }

    public void UpdateCharts(ImuPacket packet, double currentG, double currentRot) {
        MainThread.BeginInvokeOnMainThread(() => {
            lock (Sync)
            {
                AddPoint(AccX, packet.Ax);
                AddPoint(AccY, packet.Ay);
                AddPoint(AccZ, packet.Az);
                AddPoint(GyroX, packet.Gx);
                AddPoint(GyroY, packet.Gy);
                AddPoint(GyroZ, packet.Gz);
                AddPoint(QW, packet.Qw);
                AddPoint(QX, packet.Qx);
                AddPoint(QY, packet.Qy);
                AddPoint(QZ, packet.Qz);

                if (currentG > PeakGForce) PeakGForce = currentG;
                if (currentRot > PeakRotation) PeakRotation = currentRot;
            }
        });
    }

    private void AddPoint(ObservableCollection<double> list, double value)
    {
        list.Add(value);

        if (list.Count > 100)
            list.RemoveAt(0);
    }
}