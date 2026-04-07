using MotionLink.ViewModels;

namespace MotionLink.Views;

public partial class SessionDetailPage : BaseContentPage
{
	public SessionDetailPage(SessionDetailViewModel vm)
	{
		BindingContext = vm;
		InitializeComponent();
	}
}