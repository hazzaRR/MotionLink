using MotionLink.ViewModels;

namespace MotionLink.Views;

public partial class PeripheralConnectView : BaseContentPage
{
	public PeripheralConnectView(PeripheralConnectViewModel peripheralConnectViewModel)
	{
		InitializeComponent();
        BindingContext = peripheralConnectViewModel;
	}
}