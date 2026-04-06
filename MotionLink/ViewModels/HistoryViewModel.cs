using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reactive.Linq;
using MotionLink.Views;
using MotionLink.Repositories;
using Microsoft.Extensions.Logging;
using MotionLink.Models;
using CommunityToolkit.Maui.Core.Extensions;
using System.Diagnostics;
using Microsoft.Maui.Controls.Internals;

namespace MotionLink.ViewModels;


[Preserve(AllMembers = true)]
public partial class HistoryViewModel : BaseViewModel
{
    private readonly ILogger<HistoryViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<SessionOverview> _histories = [];

    private readonly IMotionLinkRepository _repo;
    public HistoryViewModel(ILogger<HistoryViewModel> logger, IMotionLinkRepository repo)
    {
        _logger = logger;
        _repo = repo;
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
            Debug.WriteLine("CRASH IN COLLECTION BINDING: " + ex);
            throw;
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