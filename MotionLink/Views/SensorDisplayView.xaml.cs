using MotionLink.ViewModels;

namespace MotionLink.Views;

public partial class SensorDisplayView : BaseContentPage
{
	public SensorDisplayView(SensorDisplayViewModel sensorDisplayViewModel)
	{
		InitializeComponent();
		BindingContext = sensorDisplayViewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		
		if (BindingContext is SensorDisplayViewModel vm)
		{
			await vm.InitializeAsync();
		}
	}
}