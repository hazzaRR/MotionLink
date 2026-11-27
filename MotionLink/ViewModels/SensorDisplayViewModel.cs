using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MotionLink.Models;
using MotionLink.Repositories;
using MotionLink.Services;
using Shiny.BluetoothLE;

namespace MotionLink.ViewModels;

public partial class SensorDisplayViewModel : BaseViewModel
{
    private const string ServiceUuid = "19B10000-E8F2-537E-4F6C-D104768A1214";
    private const string RecordingUuid = "FB7A8DB8-DE34-4C6F-9F98-D1612EE35441";
    private readonly ILogger<SensorDisplayViewModel> _logger;
    private readonly IMotionLinkRepository _repo;
    private readonly INavigationService _navigationService;
    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    private int? _currentSessionId;

    [ObservableProperty]
    private IBleService _bleService;

    [ObservableProperty]
    private bool _isCapturing;

    [ObservableProperty]
    private string? _captureError;

    [ObservableProperty]
    private IReadOnlyList<Club> _clubs = Array.Empty<Club>();

    [ObservableProperty]
    private Club? _selectedClub;

    [ObservableProperty]
    private bool _isRightHanded = Preferences.Default.Get("SwingRightHanded", true);

    [ObservableProperty]
    private Swing? _lastSavedSwing;

    partial void OnIsRightHandedChanged(bool value) => Preferences.Default.Set("SwingRightHanded", value);

    public SensorDisplayViewModel(ILogger<SensorDisplayViewModel> logger, IBleService bleService,
        IMotionLinkRepository repo, INavigationService navigationService)
    {
        _logger = logger;
        _bleService = bleService;
        _repo = repo;
        _navigationService = navigationService;
        bleService.SwingProcessingService.SwingSaved += swing =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (swing.SessionId == _currentSessionId)
                    LastSavedSwing = swing;
            });

        bleService.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IBleService.IsConnected) && !bleService.IsConnected)
                MainThread.BeginInvokeOnMainThread(async () => await PauseData());
        };
    }

    public async Task InitializeAsync()
    {
        try
        {
            if (IsCapturing) return;

            int? selectedId = SelectedClub?.Id;
            Clubs = await _repo.GetBagClubsAsync();
            SelectedClub = Clubs.FirstOrDefault(club => club.Id == selectedId)
                ?? Clubs.FirstOrDefault(club => club.Name == "7-Iron")
                ?? Clubs.FirstOrDefault();
        }
        catch (Exception ex)
        {
            CaptureError = "Could not open local recording storage.";
            _logger.LogError(ex, "Could not initialize recording storage");
        }
    }

    [RelayCommand]
    private Task NavigateToConnect() => _navigationService.NavigateToConnect();

    [RelayCommand]
    private async Task CaptureData()
    {
        await _sessionGate.WaitAsync();
        try
        {
            var peripheral = BleService.ConnectedPeripheral;
            if (_currentSessionId.HasValue || peripheral is null) return;

            if (SelectedClub is not { ShaftLengthInches: > 0 } club)
            {
                CaptureError = "Select a club before starting the session.";
                return;
            }

            bool isRightHanded = IsRightHanded;
            CaptureError = null;
            var sessionId = await _repo.CreateSessionAsync(DateTime.Now);
            try
            {
                BleService.SwingProcessingService.StartCapture(sessionId, club, isRightHanded);
                await peripheral.WriteCharacteristicAsync(ServiceUuid, RecordingUuid, [0x01]);
                if (!ReferenceEquals(peripheral, BleService.ConnectedPeripheral))
                    throw new InvalidOperationException("Sensor disconnected during session startup.");

                LastSavedSwing = null;
                _currentSessionId = sessionId;
                IsCapturing = true;
            }
            catch
            {
                await BleService.SwingProcessingService.StopCaptureAsync();
                if (ReferenceEquals(peripheral, BleService.ConnectedPeripheral))
                    BleService.DisconnectDevice();
                await _repo.DeleteSessionAsync(sessionId);
                throw;
            }
        }
        catch (Exception ex)
        {
            CaptureError = "Could not start the recording session.";
            _logger.LogError(ex, "Could not start recording");
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    [RelayCommand]
    private async Task PauseData()
    {
        await _sessionGate.WaitAsync();
        try
        {
            if (_currentSessionId is not int sessionId) return;

            var endTime = DateTime.Now;
            var writes = BleService.SwingProcessingService.StopCaptureAsync();
            try
            {
                if (BleService.ConnectedPeripheral is { } peripheral)
                    await peripheral.WriteCharacteristicAsync(ServiceUuid, RecordingUuid, [0x00]);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sensor did not acknowledge stop");
                BleService.DisconnectDevice();
            }

            var failedWrites = await writes;
            if (failedWrites > 0)
                CaptureError = $"Could not save {failedWrites} detected swing(s).";

            await _repo.UpdateSessionAsync(sessionId, $"session-{endTime}", endTime);
            _currentSessionId = null;
            IsCapturing = false;
        }
        catch (Exception ex)
        {
            CaptureError = "Could not close the session. Try ending it again.";
            _logger.LogError(ex, "Could not close recording session");
        }
        finally
        {
            _sessionGate.Release();
        }
    }
}
