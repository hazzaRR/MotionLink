using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Controls.Internals;
using MotionLink.Models;
using MotionLink.Repositories;
using MotionLink.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Text;
using System.Xml.Linq;

namespace MotionLink.ViewModels;

public partial class SwingDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ILogger<SwingDetailViewModel> _logger;

    [ObservableProperty]

    private int? _id;

    [ObservableProperty]

    private Swing? _selectedSwing;

    private readonly IMotionLinkRepository _repo;
    private ObservableCollection<double> AccX { get; } = new();
    private ObservableCollection<double> AccY { get; } = new();
    private ObservableCollection<double> AccZ { get; } = new();
    private ObservableCollection<double> GyroX { get; } = new();
    private ObservableCollection<double> GyroY { get; } = new();
    private ObservableCollection<double> GyroZ { get; } = new();

    public ISeries[] AccelSeries { get; }
    public ISeries[] GyroSeries { get; }
    public SwingDetailViewModel(ILogger<SwingDetailViewModel> logger, IMotionLinkRepository repo)
    {
        _logger = logger;
        _repo = repo;
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

    public override async Task LoadAsync()
    {
        if (Id != null)
        {
            SelectedSwing = await _repo.GetSwingByIdAsync((int)Id);
            List<ImuPacket> data = await _repo.GetRawSwingDataAsync((int)Id);

            foreach (ImuPacket packet in data) 
            {
                AccX.Add(packet.Ax);
                AccY.Add(packet.Ay);
                AccZ.Add(packet.Az);
                GyroX.Add(packet.Gx);
                GyroY.Add(packet.Gy);
                GyroZ.Add(packet.Gz);
            }
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Id = (int)query["SwingId"];
    }

}