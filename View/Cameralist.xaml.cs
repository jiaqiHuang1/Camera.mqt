using Microsoft.Maui.Controls.Maps;
using Camera.Model;
using Camera.ViewModel;
using Microsoft.Maui.Maps;

namespace Camera.View
{
    public partial class Cameralist : ContentPage
    {
        public Cameralist(CamerasViewModel viewModel)
        {
            InitializeComponent();

            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                IsVisible = true // Show the back button in the top-left corner
            });

            // Get the ViewModel
            //var viewModel = BindingContext as CamerasViewModel;
            BindingContext = viewModel;
            // Subscribe to the ViewModel's map update event
            if (viewModel != null)
            {
                viewModel.UpdateMapRequested += OnUpdateMapRequested;
            }
        }

        // Handle the map update event
        private void OnUpdateMapRequested(object sender, IEnumerable<CameraInfo> cameras)
        {
            UpdateMapPins(cameras);
        }

        // Update map pins
        private void UpdateMapPins(IEnumerable<CameraInfo> cameras)
        {
            // Clear existing pins on the map
            CameraMap.Pins.Clear();

            // Add camera location pins
            foreach (var camera in cameras)
            {
                if (double.TryParse(camera.PositionLat, out double lat) && double.TryParse(camera.PositionLong, out double lon))
                {
                    var position = new Location(lat, lon);
                    var pin = new Pin
                    {
                        Label = $"Camera {camera.Id}",
                        Location = position,
                        Type = PinType.Place
                    };
                    CameraMap.Pins.Add(pin);
                }
            }

            // If there is at least one camera, set the map center to the first camera's location
            if (cameras.Any())
            {
                var firstCamera = cameras.First();
                if (double.TryParse(firstCamera.PositionLat, out double firstLat) && double.TryParse(firstCamera.PositionLong, out double firstLon))
                {
                    var firstPosition = new Location(firstLat, firstLon);
                    CameraMap.MoveToRegion(MapSpan.FromCenterAndRadius(firstPosition, Distance.FromKilometers(10)));
                }
            }
        }
    }
}