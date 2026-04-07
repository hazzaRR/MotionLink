using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reactive.Linq;
using MotionLink.Views;
using MotionLink.Repositories;
using Microsoft.Extensions.Logging;
using MotionLink.Models;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Controls.Internals;
using MotionLink.Services;

namespace MotionLink.ViewModels;


[Preserve(AllMembers = true)]
public partial class HistoryViewModel : BaseViewModel
{
    private readonly ILogger<HistoryViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SessionOverview> _histories = [];

    [ObservableProperty] 
    private SessionOverview? _selectedSession;

    private readonly IMotionLinkRepository _repo;
    private readonly INavigationService _navigationService;
    public HistoryViewModel(ILogger<HistoryViewModel> logger, IMotionLinkRepository repo, INavigationService navigationService)
    {
        _logger = logger;
        _repo = repo;
        _navigationService = navigationService;
    }
    public bool HasHistories => Histories.Count > 0;

    public override async Task LoadAsync()
    {
        try
        {
            List<SessionOverview> result = await _repo.GetSessionsAsync();
            Histories = result.ToObservableCollection();
        }
        catch (Exception ex)
        {
            _logger.LogError($"fetching histories failed: {ex.Message}");
            throw;
        }
    }

    [RelayCommand]
    async Task NavigateToDashboard()
    {
        await _navigationService.NavigateToDashboard();
    }

    [RelayCommand]
    async Task DeleteSession(SessionOverview session)
    {
        await _repo.DeleteSessionAsync(session.Id, default);
        Histories.Remove(session);
    }

    [RelayCommand]
    private async Task NavigateToSelectedDetail()
    {
        if (SelectedSession is not null)
        {
            await _navigationService.GoToSessionDetail(SelectedSession.Id);
            SelectedSession = null;
        }
    }


}