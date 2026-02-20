using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MotionLink.ViewModels;

public partial class BaseViewModel : ObservableValidator, IBaseViewModel
{
    [ObservableProperty] 
    private bool _isLoading;

    public IAsyncRelayCommand InitializeAsyncCommand { get; }

    public BaseViewModel()
    {
        InitializeAsyncCommand = new AsyncRelayCommand(
            async () =>
            {
                IsLoading = true;
                await Loading(LoadAsync);
                IsLoading = false;
            });
    }

    protected async Task Loading(Func<Task> unitOfWork)
    {
        await unitOfWork();
    }

    public virtual Task LoadAsync()
    {
        return Task.CompletedTask;
    }
}
