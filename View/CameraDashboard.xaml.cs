using Camera.ViewModel;

namespace Camera.View;

public partial class CameraDashboard : ContentPage
{
    public CameraDashboard(CameraDashboardViewModel viewModel)
	{
        InitializeComponent();

        BindingContext = viewModel;

        viewModel.SetWebView(CameraWebView);
    }
}