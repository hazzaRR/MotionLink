using MotionLink.ViewModels;

namespace MotionLink.Views;

public partial class SwingDetailPage : BaseContentPage
{
	public SwingDetailPage(SwingDetailViewModel vm)
	{
        BindingContext = vm;
        Console.WriteLine("Stack count: " + Shell.Current.Navigation.NavigationStack.Count);
        InitializeComponent();
	}
}