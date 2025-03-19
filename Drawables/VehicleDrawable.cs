using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using Camera.Model;
using IImage = Microsoft.Maui.Graphics.IImage;
using Microsoft.Maui.Graphics.Platform;

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

            _carImage = LoadImage("car.png");
            _bicycleImage = LoadImage("bicycle.png");
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 4;
            canvas.DrawRectangle(dirtyRect);

            foreach (var vehicle in _vehicles)
            {
                IImage imageToDraw = vehicle.Type switch
                {
                    1 => _carImage,
                    2 => _bicycleImage,
                };

                if (imageToDraw != null)
                {
                    float x = (float)vehicle.X - (float)vehicle.Width / 2;
                    float y = (float)vehicle.Y - (float)vehicle.Length / 2;
                    float imageWidth = (float)vehicle.Width;
                    float imageHeight = (float)vehicle.Length;

                    canvas.DrawImage(imageToDraw, x, y, imageWidth, imageHeight);
                }
            }
        }

        private IImage LoadImage(string fileName)
        {
            try
            {
                using Stream stream = FileSystem.OpenAppPackageFileAsync(fileName).Result;
                return PlatformImage.FromStream(stream);
            }
            catch
            {
                return null;
            }
        }
    }
}
