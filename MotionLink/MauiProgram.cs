using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MotionLink.Services;
using MotionLink.ViewModels;
using MotionLink.Views;
using Shiny;
// using Syncfusion.Maui.Toolkit.Hosting;
using LiveChartsCore.SkiaSharpView.Maui;
using MotionLink.Repositories;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace MotionLink
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                // .ConfigureSyncfusionToolkit()
                .UseSkiaSharp() 
                .UseLiveCharts() 
                .UseMauiCommunityToolkit()
                .UseShiny()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
            // .RegisterRepositories()
            .RegisterServices()
            .RegisterRepositories()
            .RegisterViewModels()
            .RegisterViews();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    // private static MauiAppBuilder RegisterRepositories(this MauiAppBuilder builder)
    // {
    //     var baseUrl = DeviceInfo.Platform == DevicePlatform.Android
    //         ? "http://10.0.2.2:5191"
    //         : "https://localhost:7185";

    //     builder.Services.AddHttpClient("GloboTicketAdminApiClient",
    //         client =>
    //         {
    //             client.BaseAddress = new Uri(baseUrl);
    //             client.DefaultRequestHeaders.Add("Accept", "application/json");

    //         });

    //     builder.Services.AddTransient<IEventRepository, EventRepository>();
    //     builder.Services.AddTransient<ICategoryRepository, CategoryRepository>();
    //     return builder;
    // }

        private static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<IBleService, BleService>();
            builder.Services.AddBluetoothLE();

            // builder.Services.AddHttpClient<IGatewayClient, GatewayClient>(client =>
            // {
            //     client.DefaultRequestHeaders.Add("Accept", "application/json");
            //     client.DefaultRequestHeaders.Add("User-Agent", "MotionlinkDevice");
            // });

            return builder;
        }
        private static MauiAppBuilder RegisterRepositories(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<IMotionLinkRepository, MotionLinkRepository>();

            return builder;
        }

        private static MauiAppBuilder RegisterViewModels(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<PeripheralConnectViewModel>();
            builder.Services.AddScoped<SensorDisplayViewModel>();
            builder.Services.AddScoped<HistoryViewModel>();
            builder.Services.AddScoped<TelemetryEndpointViewModel>();
            return builder;
        }

        private static MauiAppBuilder RegisterViews(this MauiAppBuilder builder)
        {
            builder.Services.AddScoped<PeripheralConnectView>();
            builder.Services.AddScoped<SensorDisplayView>();
            builder.Services.AddScoped<HistoryView>();
            builder.Services.AddScoped<TelemetryEndpointView>();
            
            return builder;
        }
    }
}
