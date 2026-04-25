
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
    public ISwingProcessingService SwingProcessingService {get;}
    public ILiveChartService LiveChartService {get;}

    [ObservableProperty]
    private IPeripheral? _connectedPeripheral;
    public bool IsConnected => ConnectedPeripheral != null;
    public BleService(ILogger<BleService> logger, IBleManager bleManager, IPacketProcessingService packetProcessingService, ISwingProcessingService swingProcessingService, ILiveChartService liveChartService)
    {
        _logger = logger;
        _bleManager = bleManager;
        _packetProcessingService = packetProcessingService;
        SwingProcessingService = swingProcessingService;
        LiveChartService = liveChartService;
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

            var packet = _packetProcessingService.ParseData(data.Data);
            (double currentG, double currentRot) = _packetProcessingService.CalculateStats(packet);
            LiveChartService.UpdateCharts(packet, currentG, currentRot);
            SwingProcessingService.ProcessPacket(packet);
        });
        
    }
    public void DisconnectDevice()
    {
        ConnectedPeripheral?.CancelConnection();
        ConnectedPeripheral = null;
    }

}