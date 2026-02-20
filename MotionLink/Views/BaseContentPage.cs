using MotionLink.ViewModels;

namespace MotionLink.Views;

public partial class BaseContentPage : ContentPage
{
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not IBaseViewModel baseViewModel)
        {
            return;
        }

        await baseViewModel.InitializeAsyncCommand.ExecuteAsync(null);
    }
}