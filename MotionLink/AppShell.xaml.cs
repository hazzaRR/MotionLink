using MotionLink.Views;

namespace MotionLink
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            Routing.RegisterRoute(nameof(PeripheralConnectView), typeof(PeripheralConnectView));
            Routing.RegisterRoute(nameof(SensorDisplayView), typeof(SensorDisplayView));
            Routing.RegisterRoute(nameof(TelemetryEndpointView), typeof(TelemetryEndpointView));
            InitializeComponent();
        }
    }
}
