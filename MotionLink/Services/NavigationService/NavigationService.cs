using MotionLink.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace MotionLink.Services
{
    public class NavigationService : INavigationService
    {
        public async Task GoToSessionDetail(int selectedSessionId)
        {
            Dictionary<string, object> parameters = new() { { "SessionId", selectedSessionId } };
            await Shell.Current.GoToAsync(nameof(SessionDetailPage), parameters);
        }

        public async Task NavigateToDashboard()
        {
            await Shell.Current.GoToAsync($"///{nameof(SensorDisplayView)}");
        }

        public async Task NavigateToConnect()
        {
            await Shell.Current.GoToAsync($"///{nameof(PeripheralConnectView)}");
        }

        public async Task GoToSwingDetail(int selectedSwingId)
        {
            Dictionary<string, object> parameters = new() { { "SwingId", selectedSwingId } };
            await Shell.Current.GoToAsync(nameof(SessionDetailPage), parameters);
        }
    }
}
