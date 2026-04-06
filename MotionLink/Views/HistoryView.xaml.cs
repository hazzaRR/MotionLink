using MotionLink.ViewModels;

namespace MotionLink.Views;

public partial class HistoryView : BaseContentPage
{
	public HistoryView(HistoryViewModel historyViewModel)
	{
		BindingContext = historyViewModel;
		InitializeComponent();
	}
}