using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotionLink.Services;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using MotionLink.Views;

namespace MotionLink.ViewModels;

public partial class SensorDisplayViewModel : BaseViewModel
{

    [ObservableProperty]
    private IBleService _bleService;
    public SensorDisplayViewModel(IBleService bleService)
    {
        _bleService = bleService;
    }

    [RelayCommand]
    void NavigateToConnect()
    {
        Shell.Current.GoToAsync(nameof(PeripheralConnectView));
    }

    
}