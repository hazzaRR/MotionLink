using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Internals;
using MotionLink.Models;
using MotionLink.Repositories;
using MotionLink.Services;
using MotionLink.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Reactive.Linq;
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
        //if (SelectedSwing is not null)
        //{
        //    await _navigationService.GoToSwingDetail(SelectedSwing.Id);
        //    SelectedSwing = null;
        //}
    }
}