using CommunityToolkit.Mvvm.Input;

namespace MotionLink.ViewModels;

public interface IBaseViewModel
{
    IAsyncRelayCommand InitializeAsyncCommand { get; }
}
