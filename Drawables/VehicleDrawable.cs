using Microsoft.Maui.Graphics.Platform;
using Camera.Model;
using IImage = Microsoft.Maui.Graphics.IImage;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Camera.Drawables
{
    public class VehicleDrawable : IDrawable
    {
        private readonly ObservableCollection<VehiclesInfo> _vehicles;
        private readonly IImage _carImage;
        private readonly IImage _bicycleImage;

        public VehicleDrawable(ObservableCollection<VehiclesInfo> vehicles)
        {
            _vehicles = vehicles;

            // Load images from MAUI resources
            _carImage = LoadImage("car.png");
            _bicycleImage = LoadImage("bicycle.png");
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {

            if (_vehicles.Count == 0)
                return; // 没数据时不画
            // Draw a black border around the GraphicsView
            canvas.StrokeColor = Colors.Black;  // Set border color
            canvas.StrokeSize = 4;              // Set border thickness
            canvas.DrawRectangle(dirtyRect);    // Draw the border

            

            foreach (var vehicle in _vehicles)
            {
                // Select the appropriate image based on vehicle type
                IImage imageToDraw = vehicle.Type switch
                {
                    1 => _carImage,      // Car 
                    2 => _bicycleImage,  // Bicycle
                };


                if (imageToDraw != null)
                {
                    // Adjust image size based on database width and height
                    float imageWidth = (float)vehicle.Width;
                    float imageHeight = (float)vehicle.Length;

                    // Calculate the center point for drawing the image
                    float x = (float)vehicle.X - imageWidth / 2;
                    float y = (float)vehicle.Y - imageHeight / 2;



                    // Draw the scaled image
                    canvas.DrawImage(imageToDraw, x, y, imageWidth, imageHeight);
                }
            }


        }

        private IImage LoadImage(string fileName)
        {
            try
            {
                var assembly = GetType().GetTypeInfo().Assembly;
                var resourceName = $"Camera.Resources.Images.{fileName}"; // Full resource path

                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                var image = PlatformImage.FromStream(stream);

                return image;
            }
            catch (Exception ex)
            {
                return null;
            }

            
        }
    }
}