using Camera.View;
namespace Camera
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Cameralist), typeof(Cameralist)); // explicit registration Cameralist
            Routing.RegisterRoute(nameof(Setting_Wlan), typeof(Setting_Wlan)); // explicit registration Cameralist
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage)); // explicit registration Cameralist
            Routing.RegisterRoute(nameof(LivePage), typeof(LivePage)); // explicit registration Cameralist

        }
    }
}
