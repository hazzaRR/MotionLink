using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MotionLink.Services;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using MotionLink.Views;
using Microsoft.Extensions.Logging;
using Android.Util;
using System.Net.Http.Json;

namespace MotionLink.ViewModels;

public partial class TelemetryEndpointViewModel : BaseViewModel
{

    private readonly ILogger<TelemetryEndpointViewModel> _logger;

    [ObservableProperty]
    private string _url = string.Empty;

    private readonly HttpClient _client;
    public TelemetryEndpointViewModel(ILogger<TelemetryEndpointViewModel> logger, HttpClient client)
    {
        _logger = logger;
        _client = client;
    }

    // [RelayCommand]
    // public async Task SendData()
    // {
    //     try
    //     {
    //         await _client.PostAsJsonAsync(Url, "test data");
    //     }
    //     catch (Exception ex)
    //     {
    //     }
    // }

    
}