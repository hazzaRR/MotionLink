using MotionLink.ViewModels;

namespace MotionLink.Views;

public partial class TelemetryEndpointView : BaseContentPage
{
	public TelemetryEndpointView(TelemetryEndpointViewModel telemetryEndpointViewModel)
	{
		InitializeComponent();
		BindingContext = telemetryEndpointViewModel;
	}
}