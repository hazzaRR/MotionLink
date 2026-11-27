using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE;

namespace MotionLink.Services;

public partial class BleService : ObservableObject, IBleService
{
    private readonly ILogger<BleService> _logger;
    private readonly IBleManager _bleManager;
    private readonly IPacketProcessingService _packetProcessingService;
    private const string GolfServiceUuid = "19B10000-E8F2-537E-4F6C-D104768A1214";
    private const string TelemetryUuid = "36151377-DDD7-4C2F-9207-0724C52C3CCC";
    private const string RecordingUuid = "FB7A8DB8-DE34-4C6F-9F98-D1612EE35441";
    private const string LastPeripheralKey = "MotionLink.LastPeripheral";
    private const string BatteryServiceUuid = "0000180f-0000-1000-8000-00805f9b34fb";
    private const string BatteryLevelUuid = "00002a19-0000-1000-8000-00805f9b34fb";
    private readonly SerialDisposable _subscriptions = new();
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private readonly SemaphoreSlim _gattGate = new(1, 1);
    private IObservable<ScanResult> _golfScan;
    private CancellationTokenSource? _reconnect;
    private bool _isForeground;
    private bool _manualConnect;

    [ObservableProperty]
    private int? _connectedRssi;

    [ObservableProperty]
    private int? _batteryPercent;

    [ObservableProperty]
    private bool _isReconnecting;
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
        _golfScan = CreateGolfScan();
        _packetProcessingService = packetProcessingService;
        SwingProcessingService = swingProcessingService;
        LiveChartService = liveChartService;
    }

    public IObservable<IPeripheral> ScanForDevices(string serviceUuid)
        => (serviceUuid == GolfServiceUuid ? ScanWithSignal() : _bleManager.Scan(new ScanConfig { ServiceUuids = [serviceUuid] }))
            .Select(result => result.Peripheral);

    public IObservable<ScanResult> ScanWithSignal() => Observable.Defer(() => _golfScan);

    private IObservable<ScanResult> CreateGolfScan()
        => Observable.Defer(() => _bleManager.Scan(new ScanConfig { ServiceUuids = [GolfServiceUuid] }))
            .Do(_ => { }, _ => _golfScan = CreateGolfScan(), () => _golfScan = CreateGolfScan())
            .Publish().RefCount();

    public async Task ConnectAsync(IPeripheral peripheral)
    {
        _manualConnect = true;
        CancelReconnect();
        try
        {
            await ConnectPeripheralAsync(peripheral, CancellationToken.None);
            if (ReferenceEquals(ConnectedPeripheral, peripheral))
                Preferences.Default.Set(LastPeripheralKey, peripheral.Uuid);
        }
        finally
        {
            _manualConnect = false;
            BeginReconnect();
        }
    }

    private async Task ConnectPeripheralAsync(IPeripheral peripheral, CancellationToken cancellationToken)
    {
        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected) return;
            await OpenConnectionAsync(peripheral, cancellationToken);
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    private async Task OpenConnectionAsync(IPeripheral peripheral, CancellationToken cancellationToken)
    {

        var subscriptions = new CompositeDisposable();
        _subscriptions.Disposable = subscriptions;
        try
        {
            await peripheral.ConnectAsync().WaitAsync(cancellationToken);
            var mtu = await peripheral.TryRequestMtuAsync(247).WaitAsync(cancellationToken);
            _logger.LogInformation("Negotiated BLE MTU: {Mtu}", mtu);

            using var setupTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            setupTimeout.CancelAfter(TimeSpan.FromSeconds(10));
            var setupToken = setupTimeout.Token;
            var services = await peripheral.GetServicesAsync(setupToken);
            _logger.LogInformation("Discovered BLE services: {Services}", string.Join(", ", services.Select(service => service.Uuid)));

            long? previousTimestamp = null;
            long? firstTimestamp = null;
            int received = 0, gaps = 0, malformed = 0;
            double peakG = 0, peakRotation = 0;
            long lastChartUpdate = Environment.TickCount64 - 25;

            subscriptions.Add(peripheral.NotifyCharacteristic(
                GolfServiceUuid, TelemetryUuid)
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
                    MainThread.BeginInvokeOnMainThread(() => HandleConnectionLost(peripheral, subscriptions));
                }));

            // Complete the notification descriptor write before polling or accepting recording commands.
            await peripheral.WaitForCharacteristicSubscriptionAsync(GolfServiceUuid, TelemetryUuid, setupToken);
            var recordingState = await peripheral.ReadCharacteristic(GolfServiceUuid, RecordingUuid)
                .FirstAsync().Timeout(TimeSpan.FromSeconds(3)).ToTask(setupToken);
            if (recordingState.Data is not [0 or 1])
                throw new InvalidOperationException("The sensor returned an invalid recording characteristic.");

            setupToken.ThrowIfCancellationRequested();
            ConnectedPeripheral = peripheral;
            subscriptions.Add(peripheral.WhenStatusChanged().Subscribe(status =>
            {
                if (status == ConnectionState.Disconnected)
                    MainThread.BeginInvokeOnMainThread(() => HandleConnectionLost(peripheral, subscriptions));
            }));

            var statusCancellation = new CancellationDisposable();
            var statusToken = statusCancellation.Token;
            subscriptions.Add(statusCancellation);
            _ = RefreshConnectedStatusAsync(peripheral, statusToken);
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
        Preferences.Default.Remove(LastPeripheralKey);
        CancelReconnect();
        CloseConnection();
    }

    public void SetForeground(bool isForeground)
    {
        _isForeground = isForeground;
        if (isForeground)
            BeginReconnect();
        else
            CancelReconnect();
    }

    private void HandleConnectionLost(IPeripheral peripheral, CompositeDisposable subscriptions)
    {
        if (ReferenceEquals(ConnectedPeripheral, peripheral) &&
            ReferenceEquals(_subscriptions.Disposable, subscriptions))
            RecoverConnection();
    }

    public void RecoverConnection()
    {
        CloseConnection();
        BeginReconnect();
    }

    private void CloseConnection()
    {
        var peripheral = ConnectedPeripheral;
        ConnectedPeripheral = null;
        ConnectedRssi = null;
        BatteryPercent = null;
        _subscriptions.Disposable = null;
        peripheral?.CancelConnection();
    }

    private void BeginReconnect()
    {
        if (!_isForeground || IsConnected || _manualConnect || _reconnect is not null) return;
        string uuid = Preferences.Default.Get(LastPeripheralKey, string.Empty);
        if (uuid.Length == 0) return;

        _reconnect = new CancellationTokenSource();
        IsReconnecting = true;
        _ = ReconnectAsync(uuid, _reconnect);
    }

    private void CancelReconnect()
    {
        var pending = _reconnect;
        _reconnect = null;
        IsReconnecting = false;
        pending?.Cancel();
    }

    private async Task ReconnectAsync(string uuid, CancellationTokenSource cancellation)
    {
        var token = cancellation.Token;
        try
        {
            while (!token.IsCancellationRequested && !IsConnected)
            {
                try
                {
                    // Wait until Bluetooth is available without showing a permission dialog on launch.
                    if (_bleManager.CurrentAccess == Shiny.AccessState.Available)
                    {
                        var result = await ScanWithSignal().FirstAsync(scan => scan.Peripheral.Uuid == uuid)
                            .Timeout(TimeSpan.FromSeconds(10)).ToTask(token);
                        await ConnectPeripheralAsync(result.Peripheral, token);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Reconnect to {PeripheralUuid} did not complete", uuid);
                }

                if (!IsConnected)
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_reconnect, cancellation))
            {
                _reconnect = null;
                IsReconnecting = false;
            }
            cancellation.Dispose();
        }
    }

    public async Task SetRecordingAsync(IPeripheral peripheral, bool recording)
    {
        byte expected = recording ? (byte)1 : (byte)0;
        await _gattGate.WaitAsync();
        try
        {
            if (!ReferenceEquals(ConnectedPeripheral, peripheral))
                throw new InvalidOperationException("The sensor is no longer connected.");

            try
            {
                await peripheral.WriteCharacteristic(GolfServiceUuid, RecordingUuid, [expected], withResponse: true)
                    .FirstAsync().Timeout(TimeSpan.FromSeconds(3)).ToTask();
            }
            catch (Exception writeError) when (ReferenceEquals(ConnectedPeripheral, peripheral))
            {
                // The peripheral may have applied a write even when the response was lost.
                try
                {
                    var state = await peripheral.ReadCharacteristic(GolfServiceUuid, RecordingUuid)
                        .FirstAsync().Timeout(TimeSpan.FromSeconds(2)).ToTask();
                    if (state.Data is [var actual] && actual == expected)
                        return;
                }
                catch (Exception readError)
                {
                    _logger.LogDebug(readError, "Could not verify sensor recording state");
                }

                throw new InvalidOperationException("Sensor did not confirm the recording command.", writeError);
            }
        }
        finally
        {
            _gattGate.Release();
        }
    }

    private async Task RefreshConnectedStatusAsync(IPeripheral peripheral, CancellationToken token)
    {
        int cycle = 0;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await _gattGate.WaitAsync(token);
                try
                {
                    await ReadConnectedStatusAsync(peripheral, readBattery: cycle++ % 6 == 0, token);
                }
                finally
                {
                    _gattGate.Release();
                }

                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    private async Task ReadConnectedStatusAsync(IPeripheral peripheral, bool readBattery, CancellationToken token)
    {
        try
        {
            var rssi = await peripheral.ReadRssi().FirstAsync().Timeout(TimeSpan.FromSeconds(3)).ToTask(token);
            if (!token.IsCancellationRequested) ConnectedRssi = rssi;
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Could not read connected RSSI");
        }

        if (readBattery && !token.IsCancellationRequested)
        {
            try
            {
                var result = await peripheral.ReadCharacteristic(BatteryServiceUuid, BatteryLevelUuid)
                    .FirstAsync().Timeout(TimeSpan.FromSeconds(3)).ToTask(token);
                if (!token.IsCancellationRequested)
                    BatteryPercent = result.Data is [<= 100] ? result.Data[0] : null;
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                _logger.LogDebug(ex, "Battery Service unavailable");
                if (ReferenceEquals(ConnectedPeripheral, peripheral)) BatteryPercent = null;
            }
        }
    }
}
