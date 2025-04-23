using Camera.View;
namespace Camera
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(CameraDashboard), typeof(CameraDashboard)); // explicit registration CameraDashboard
            Routing.RegisterRoute(nameof(Setting_Wlan), typeof(Setting_Wlan)); // explicit registration Cameralist
            Routing.RegisterRoute(nameof(StreamPage), typeof(StreamPage)); // explicit registration Cameralist
            Routing.RegisterRoute(nameof(LivePage), typeof(LivePage)); // explicit registration Cameralist

        }
    }
}
