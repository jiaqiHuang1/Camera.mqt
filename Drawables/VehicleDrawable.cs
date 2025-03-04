using Microsoft.Maui.Graphics.Platform;
using Camera.Model;
using IImage = Microsoft.Maui.Graphics.IImage;
using System.Collections.ObjectModel;

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

            // ✅ 从 MAUI 资源目录加载图片
            _carImage = LoadImage("car.png");
            _bicycleImage = LoadImage("bicycle.png");
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // ✅ 1. 先绘制黑色边框
            canvas.StrokeColor = Colors.Black;  // 设定边框颜色
            canvas.StrokeSize = 4;              // 设定边框厚度
            canvas.DrawRectangle(dirtyRect);    // 画出整个 `GraphicsView` 的边框

            foreach (var vehicle in _vehicles)
            {
                // 选择汽车或自行车图片
                IImage imageToDraw = vehicle.Type switch
                {
                    1 => _carImage,      // 汽车 🚗
                    2 => _bicycleImage,  // 自行车 🚲
                };

                if (imageToDraw != null)
                {
                    // 🚀 **根据数据库 `width` 和 `height` 调整图片大小**
                    float imageWidth = (float)vehicle.Width;
                    float imageHeight = (float)vehicle.Length;

                    // 计算图片绘制的中心点
                    float x = (float)vehicle.X - imageWidth / 2;
                    float y = (float)vehicle.Y - imageHeight / 2;

                    // ✅ **绘制缩放后的图片**
                    canvas.DrawImage(imageToDraw, x, y, imageWidth, imageHeight);
                }
            }
        }

        private IImage LoadImage(string fileName)
        {
            try
            {
                // ✅ 直接从 MAUI 资源目录加载图片
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
