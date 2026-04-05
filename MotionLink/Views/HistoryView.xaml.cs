using MotionLink.ViewModels;

namespace MotionLink.Views;

public partial class HistoryView : BaseContentPage
{
	public HistoryView(HistoryViewModel historyViewModel)
	{
		InitializeComponent();
		BindingContext = historyViewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (BindingContext is HistoryViewModel vm)
		{
			await vm.InitializeAsync();
		}
	}
}