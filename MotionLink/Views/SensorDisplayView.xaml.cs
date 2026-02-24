using MotionLink.ViewModels;

namespace MotionLink.Views;

public partial class SensorDisplayView : BaseContentPage
{
	public SensorDisplayView(SensorDisplayViewModel sensorDisplayViewModel)
	{
		InitializeComponent();
		BindingContext = sensorDisplayViewModel;
	}
}