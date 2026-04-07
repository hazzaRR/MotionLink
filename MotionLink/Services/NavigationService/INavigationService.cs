using System;
using System.Collections.Generic;
using System.Text;

namespace MotionLink.Services
{
    public interface INavigationService
    {
        Task GoToSessionDetail(int selectedSessionId);
        Task GoToSwingDetail(int selectedSwingId);
        Task NavigateToDashboard();
        Task NavigateToConnect();
    }
}
