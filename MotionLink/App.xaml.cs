using MotionLink.Services;

namespace MotionLink
{
    public partial class App : Application
    {
        private readonly IBleService _bleService;

        public App(IBleService bleService)
        {
            _bleService = bleService;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.Activated += (_, _) => _bleService.SetForeground(true);
            window.Resumed += (_, _) => _bleService.SetForeground(true);
            window.Stopped += (_, _) => _bleService.SetForeground(false);
            window.Destroying += (_, _) => _bleService.SetForeground(false);
            return window;
        }
    }
}