using MauiApp1.ViewModels;

namespace MauiApp1;

public partial class ProcessManagerPage : ContentPage
{
	public ProcessManagerPage(ProcessManagerViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}