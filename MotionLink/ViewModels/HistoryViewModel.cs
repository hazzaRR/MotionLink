using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotionLink.Services;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using MotionLink.Views;

namespace MotionLink.ViewModels;

public partial class HistoryViewModel : BaseViewModel
{

    public bool HasHistories => Histories.Count > 0;

    [ObservableProperty]
    private ObservableCollection<string> _histories = new();

    public HistoryViewModel()
    {
    }


    [RelayCommand]
    async Task NavigateToDashboard()
    {
        await Shell.Current.GoToAsync($"///{nameof(SensorDisplayView)}");
    }


}