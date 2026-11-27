using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using MotionLink.Models;
using MotionLink.Repositories;
using SkiaSharp;

namespace MotionLink.ViewModels;

public partial class SwingDetailViewModel : BaseViewModel, IQueryAttributable
{
    private const int MaxDisplayPointsPerAxis = 800;
    private readonly IMotionLinkRepository _repo;

    [ObservableProperty]
    private int? _id;

    [ObservableProperty]
    private Swing? _selectedSwing;

    [ObservableProperty]
    private ISeries[] _accelSeries = [];

    [ObservableProperty]
    private ISeries[] _gyroSeries = [];

    [ObservableProperty]
    private Axis[] _accelTimeAxis = [];

    [ObservableProperty]
    private Axis[] _gyroTimeAxis = [];

    [ObservableProperty]
    private RectangularSection[] _accelMarkers = [];

    [ObservableProperty]
    private RectangularSection[] _gyroMarkers = [];

    [ObservableProperty]
    private PhaseMarker[] _phaseMarkers = [];

    public SwingDetailViewModel(ILogger<SwingDetailViewModel> logger, IMotionLinkRepository repo)
    {
        _repo = repo;
    }

    public override async Task LoadAsync()
    {
        var id = Id;
        if (id is null) return;

        var swing = await _repo.GetSwingByIdAsync(id.Value);
        if (Id != id) return;
        SelectedSwing = swing;

        var samples = swing?.Data.OrderBy(packet => packet.TimeStamp).ToArray() ?? [];
        DateTime first = samples.FirstOrDefault()?.TimeStamp ?? default;
        DateTime last = samples.LastOrDefault()?.TimeStamp ?? default;
        bool hasTimeAxis = first != default && last > first;

        double[] positions = samples.Select((packet, index) =>
            hasTimeAxis ? (packet.TimeStamp - first).TotalSeconds : index).ToArray();

        AccelSeries =
        [
            CreateLine("Acc X", SKColors.Blue, positions, samples.Select(packet => packet.Ax)),
            CreateLine("Acc Y", SKColors.Red, positions, samples.Select(packet => packet.Ay)),
            CreateLine("Acc Z", SKColors.Green, positions, samples.Select(packet => packet.Az))
        ];
        GyroSeries =
        [
            CreateLine("Gyro X", SKColors.Blue, positions, samples.Select(packet => packet.Gx)),
            CreateLine("Gyro Y", SKColors.Red, positions, samples.Select(packet => packet.Gy)),
            CreateLine("Gyro Z", SKColors.Green, positions, samples.Select(packet => packet.Gz))
        ];
        AccelTimeAxis = [new Axis { Name = hasTimeAxis ? "Elapsed time (s)" : "Sample" }];
        GyroTimeAxis = [new Axis { Name = hasTimeAxis ? "Elapsed time (s)" : "Sample" }];

        PhaseMarkers = hasTimeAxis && swing is not null ? CreatePhaseMarkers(swing, first, last) : [];
        AccelMarkers = PhaseMarkers.Select(CreateSection).ToArray();
        GyroMarkers = PhaseMarkers.Select(CreateSection).ToArray();
    }

    private static LineSeries<ObservablePoint> CreateLine(string name, SKColor color,
        double[] positions, IEnumerable<double> values) => new()
    {
        Name = name,
        Values = CreateDisplayPoints(positions, values.ToArray()),
        IsHoverable = false,
        GeometrySize = 0,
        LineSmoothness = 0,
        Fill = null,
        Stroke = new SolidColorPaint(color) { StrokeThickness = 2 }
    };

    private static ObservablePoint[] CreateDisplayPoints(double[] positions, double[] values)
    {
        if (values.Length <= MaxDisplayPointsPerAxis)
            return positions.Zip(values, (x, y) => new ObservablePoint(x, y)).ToArray();

        // Keep each bucket's minimum and maximum in time order so brief spikes stay visible.
        int bucketCount = (MaxDisplayPointsPerAxis - 2) / 2;
        var points = new List<ObservablePoint>(MaxDisplayPointsPerAxis) { new(positions[0], values[0]) };
        for (int bucket = 0; bucket < bucketCount; bucket++)
        {
            int start = 1 + (int)((long)bucket * (values.Length - 2) / bucketCount);
            int end = 1 + (int)((long)(bucket + 1) * (values.Length - 2) / bucketCount);
            int lowest = start;
            int highest = start;
            for (int i = start + 1; i < end; i++)
            {
                if (values[i] < values[lowest]) lowest = i;
                if (values[i] > values[highest]) highest = i;
            }

            int first = Math.Min(lowest, highest);
            int last = Math.Max(lowest, highest);
            points.Add(new ObservablePoint(positions[first], values[first]));
            if (first != last)
                points.Add(new ObservablePoint(positions[last], values[last]));
        }

        points.Add(new ObservablePoint(positions[^1], values[^1]));
        return points.ToArray();
    }

    private static PhaseMarker[] CreatePhaseMarkers(Swing swing, DateTime first, DateTime last)
    {
        (string Name, DateTime? Time, SKColor Color)[] phases =
        [
            ("Address", swing.AddressTime, SKColors.SlateGray),
            ("Takeaway", swing.TakeawayTime, SKColors.Teal),
            ("Top", swing.TopTime, SKColors.MediumPurple),
            ("Impact", swing.ImpactTime, SKColors.DarkOrange),
            ("Finish", swing.FinishTime, SKColors.DeepPink)
        ];

        var markers = new List<PhaseMarker>();
        foreach (var phase in phases)
        {
            if (phase.Time is DateTime time && time >= first && time <= last)
                markers.Add(new PhaseMarker(phase.Name, (time - first).TotalSeconds, phase.Color));
        }
        return markers.ToArray();
    }

    private static RectangularSection CreateSection(PhaseMarker marker) => new()
    {
        Xi = marker.Seconds,
        Xj = marker.Seconds,
        Stroke = new SolidColorPaint(marker.StrokeColor) { StrokeThickness = 2 },
        ZIndex = 10
    };

    public sealed record PhaseMarker(string Name, double Seconds, SKColor StrokeColor)
    {
        public string Caption => $"{Name} {Seconds:F3}s";
        public Color Color => Color.FromRgb(StrokeColor.Red, StrokeColor.Green, StrokeColor.Blue);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Id = (int)query["SwingId"];
    }
}
