using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Internals;
using MotionLink.Models;
using MotionLink.Repositories;
using MotionLink.Services;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Text;
using System.Xml.Linq;

namespace MotionLink.ViewModels;

public partial class SessionDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ILogger<SessionDetailViewModel> _logger;

    [ObservableProperty]

    private int? _id;

    [ObservableProperty]

    private Session? _selectedSession;

    [ObservableProperty]

    private Swing? _selectedSwing;

    [ObservableProperty]
    private ObservableCollection<Swing> _swings = [];

    private readonly IMotionLinkRepository _repo;

    private readonly INavigationService _navigationService;

    public SessionDetailViewModel(ILogger<SessionDetailViewModel> logger, IMotionLinkRepository repo, INavigationService navigationService)
    {
        _logger = logger;
        _repo = repo;
        _navigationService = navigationService;
    }

    public override async Task LoadAsync()
    {
        if (Id != null)
        {
            SelectedSession = await _repo.GetSessionByIdAsync((int)Id);

            Swings = SelectedSession?.Swings?.ToObservableCollection();
        }
        //await Loading(
        //    async () =>
        //    {
        //        if (_id != null)
        //        {
        //            _selectedSession = await _repo.GetSessionByIdAsync((int)_id);
        //        }
        //    });
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Id = (int)query["SessionId"];
    }

    [RelayCommand]
    private async Task NavigateToSelectedSwing()
    {
        if (SelectedSwing is not null)
        {
            await _navigationService.GoToSwingDetail(SelectedSwing.Id);
            SelectedSwing = null;
        }
    }

    [RelayCommand]
    async Task DeleteSwing(Swing swing)
    {
        await _repo.DeleteSwingAsync(swing.Id, default);
        Swings.Remove(swing);
    }

    [RelayCommand]
    async Task ShareSwing(Swing swing)
    {
        List<ImuPacket> data = await _repo.GetRawSwingDataAsync(swing.Id, default);
        string fileName = $"{SelectedSession.Name.Replace("/", "-").Replace("\\", "-").Replace(":", "-").Replace(" ", "_")}-swing.csv";




        string file = Path.Combine(FileSystem.CacheDirectory, fileName);

        var csvString = new StringBuilder();
        csvString.AppendLine("timestamp,ax,ay,az,gx,gy,gz");

        foreach (var packet in data)
        {
            csvString.AppendLine($"{packet.TimeStamp:o},{packet.Ax},{packet.Ay},{packet.Az},{packet.Gx},{packet.Gy},{packet.Gz}");
        }


        File.WriteAllText(file, csvString.ToString());

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share raw swing data",
            File = new ShareFile(file, "text/csv")
        });
    }

    [RelayCommand]
    async Task DownloadSwing(Swing swing)
    {
        bool canSave = await RequestStoragePermissionsAndSaveFile();
        if (canSave)
        {
            List<ImuPacket> data = await _repo.GetRawSwingDataAsync(swing.Id, default);
            string fileName = $"{SelectedSession.Name.Replace("/", "-").Replace("\\", "-").Replace(":", "-").Replace(" ", "_")}-swing.csv";

            var csvString = new StringBuilder();
            csvString.AppendLine("timestamp,ax,ay,az,gx,gy,gz");

            foreach (var packet in data)
            {
                csvString.AppendLine($"{packet.TimeStamp:o},{packet.Ax},{packet.Ay},{packet.Az},{packet.Gx},{packet.Gy},{packet.Gz}");
            }

            using var stream = new MemoryStream(Encoding.Default.GetBytes(csvString.ToString()));
            var fileSaverResult = await FileSaver.Default.SaveAsync(fileName, stream);
            if (fileSaverResult.IsSuccessful)
            {
                await Toast.Make($"The file was saved successfully to location: {fileSaverResult.FilePath}").Show();
            }
            else
            {
                await Toast.Make($"The file was not saved successfully with error: {fileSaverResult.Exception.Message}").Show();
            }
        }
    }

    private async Task<bool> RequestStoragePermissionsAndSaveFile(CancellationToken cancellationToken = default)
    {
        var readPermissionStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
        var writePermissionStatus = await Permissions.RequestAsync<Permissions.StorageWrite>();

        if (readPermissionStatus != PermissionStatus.Granted ||
            writePermissionStatus != PermissionStatus.Granted)
        {
            await Toast
                .Make("Storage permissions are required to save files.")
                .Show(cancellationToken);

            return false;
        }

        return true;
    }
}