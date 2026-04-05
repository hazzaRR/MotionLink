using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotionLink.Services;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using MotionLink.Views;
using MotionLink.Repositories;
using Microsoft.Extensions.Logging;
using MotionLink.Models;
using System.Reactive.Threading.Tasks;
using CommunityToolkit.Maui.Core.Extensions;

namespace MotionLink.ViewModels;

public partial class HistoryViewModel : BaseViewModel
{
    private readonly ILogger<HistoryViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SessionOverview> _histories = new();
    private readonly IMotionLinkRepository _repo;
    public HistoryViewModel(ILogger<HistoryViewModel> logger, IMotionLinkRepository repo)
    {
        _logger = logger;
        _repo = repo;
    }
    public bool HasHistories => Histories.Count > 0;

    public async Task InitializeAsync(CancellationToken stoppingToken = default)
    {
        try
        {
            List<SessionOverview> result = await _repo.GetSessionsAsync(stoppingToken);
            Histories = result.ToObservableCollection();
        }
        catch (Exception ex)
        {
            _logger.LogError($"fetching histories failed: {ex.Message}");
        }
    }


    [RelayCommand]
    async Task NavigateToDashboard()
    {
        await Shell.Current.GoToAsync($"///{nameof(SensorDisplayView)}");
    }

    [RelayCommand]
    async Task DeleteSession(SessionOverview session)
    {
        await _repo.DeleteSessionAsync(session.Id, default);
        Histories.Remove(session);
    }


}