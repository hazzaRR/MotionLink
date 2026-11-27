using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE;

namespace MotionLink.Services;

public partial class BleService : ObservableObject, IBleService
{
    private readonly ILogger<BleService> _logger;
    private readonly IBleManager _bleManager;
    private readonly IPacketProcessingService _packetProcessingService;
    private readonly SerialDisposable _subscriptions = new();
    public ISwingProcessingService SwingProcessingService { get; }
    public ILiveChartService LiveChartService { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    private IPeripheral? _connectedPeripheral;
    public bool IsConnected => ConnectedPeripheral is not null;

    public BleService(ILogger<BleService> logger, IBleManager bleManager,
        IPacketProcessingService packetProcessingService, ISwingProcessingService swingProcessingService,
        ILiveChartService liveChartService)
    {
        _logger = logger;
        _bleManager = bleManager;
        _packetProcessingService = packetProcessingService;
        SwingProcessingService = swingProcessingService;
        LiveChartService = liveChartService;
    }

    public IObservable<IPeripheral> ScanForDevices(string serviceUuid)
        => _bleManager.Scan(new ScanConfig { ServiceUuids = [serviceUuid] }).Select(x => x.Peripheral);

    public async Task ConnectAsync(IPeripheral peripheral)
    {
        if (IsConnected) return;

        var subscriptions = new CompositeDisposable();
        _subscriptions.Disposable = subscriptions;
        try
        {
            await peripheral.ConnectAsync();
            var mtu = await peripheral.TryRequestMtuAsync(247);
            _logger.LogInformation("Negotiated BLE MTU: {Mtu}", mtu);

            long? previousTimestamp = null;
            long? firstTimestamp = null;
            int received = 0, gaps = 0, malformed = 0;
            double peakG = 0, peakRotation = 0;
            long lastChartUpdate = Environment.TickCount64 - 25;

            subscriptions.Add(peripheral.NotifyCharacteristic(
                "19B10000-E8F2-537E-4F6C-D104768A1214", "36151377-DDD7-4C2F-9207-0724C52C3CCC")
                .Subscribe(notification =>
                {
                    if (!ReferenceEquals(ConnectedPeripheral, peripheral)) return;

                    if (notification.Data is not { Length: 24 or 41 } data)
                    {
                        if (++malformed % 200 == 1)
                            _logger.LogWarning("Dropped IMU notification of {Length} bytes ({Count} malformed)",
                                notification.Data?.Length ?? 0, malformed);
                        return;
                    }

                    var packet = _packetProcessingService.ParseData(data);
                    if (packet.SensorTimestampMs is long timestamp)
                    {
                        if (previousTimestamp is long previous)
                        {
                            uint delta = unchecked((uint)timestamp - (uint)previous);
                            if (delta == 0 || delta > int.MaxValue) return;
                            if (delta > 7) gaps++;
                        }

                        previousTimestamp = timestamp;
                        firstTimestamp ??= timestamp;
                        if (++received == 200)
                        {
                            uint duration = unchecked((uint)timestamp - (uint)firstTimestamp.Value);
                            _logger.LogInformation("Received {Count} IMU samples over {DurationMs} ms with {Gaps} timing gaps",
                                received, duration, gaps);
                            received = gaps = 0;
                            firstTimestamp = null;
                        }
                    }

                    SwingProcessingService.ProcessPacket(packet);
                    var (currentG, currentRotation) = _packetProcessingService.CalculateStats(packet);
                    peakG = Math.Max(peakG, currentG);
                    peakRotation = Math.Max(peakRotation, currentRotation);
                    long now = Environment.TickCount64;
                    if (now - lastChartUpdate >= 25)
                    {
                        LiveChartService.UpdateCharts(packet, peakG, peakRotation);
                        peakG = peakRotation = 0;
                        lastChartUpdate = now;
                    }
                }, error =>
                {
                    _logger.LogError(error, "IMU notification stream failed");
                    if (ReferenceEquals(ConnectedPeripheral, peripheral))
                        DisconnectDevice();
                }));

            ConnectedPeripheral = peripheral;
            subscriptions.Add(peripheral.WhenStatusChanged().Subscribe(status =>
            {
                if (status == ConnectionState.Disconnected && ReferenceEquals(ConnectedPeripheral, peripheral))
                {
                    ConnectedPeripheral = null;
                    subscriptions.Dispose();
                }
            }));
        }
        catch
        {
            subscriptions.Dispose();
            if (ReferenceEquals(ConnectedPeripheral, peripheral))
                ConnectedPeripheral = null;
            peripheral.CancelConnection();
            throw;
        }
    }

    public void DisconnectDevice()
    {
        var peripheral = ConnectedPeripheral;
        ConnectedPeripheral = null;
        _subscriptions.Disposable = null;
        peripheral?.CancelConnection();
    }
}
