using MotionLink.Views;

namespace MotionLink
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            //Routing.RegisterRoute(nameof(PeripheralConnectView), typeof(PeripheralConnectView));
            //Routing.RegisterRoute(nameof(SensorDisplayView), typeof(SensorDisplayView));
            //Routing.RegisterRoute(nameof(HistoryView), typeof(HistoryView));
            Routing.RegisterRoute(nameof(SessionDetailPage), typeof(SessionDetailPage));
            Routing.RegisterRoute(nameof(SwingDetailPage), typeof(SwingDetailPage));
            // Routing.RegisterRoute(nameof(TelemetryEndpointView), typeof(TelemetryEndpointView));
            InitializeComponent();
        }
    }
}
