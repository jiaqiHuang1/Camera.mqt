using Microsoft.Maui.Graphics;
using System;
using Camera.ViewModel;

namespace Camera.Drawables
{
    public class ArcSlider_VerDrawable : IDrawable
    {
        private readonly ArcSlider_VerViewModel _viewModel;
        public float _outerRadius;
        public float _innerRadius;
        public float _centerX;
        public float _centerY;
        private float _handleRadius;
        public float _arcWidth = 20; // Fixed arc width

        public ArcSlider_VerDrawable(ArcSlider_VerViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // 1. Draw the white background for better visibility
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            // 2. Calculate the center point of GraphicsView
            float viewWidth = dirtyRect.Width;
            float viewHeight = dirtyRect.Height;

            _centerX = viewWidth / 2 -60;
            _centerY = viewHeight / 2 - 70;

            _outerRadius = viewWidth / 2 - 20;
            _innerRadius = _outerRadius - _arcWidth;
            _handleRadius = _arcWidth / 2;

            //3. Draw auxiliary lines (cross) to mark the center point
            //canvas.StrokeColor = Colors.White;
            //canvas.StrokeSize = 2;
            //canvas.DrawLine(_centerX, 0, _centerX, viewHeight); // Vertical line
            //canvas.DrawLine(0, _centerY, viewWidth, _centerY); // Horizontal line

            //4. Draw the center point marker
            //canvas.FillColor = Colors.Yellow;
            //canvas.FillCircle(_centerX, _centerY, 5);

            // 5. Fix: Recalculate the starting point for DrawArc()
            float arcLeft = _centerX - _outerRadius;
            float arcTop = _centerY - _outerRadius;

            // 6. Draw the arc (open at the bottom)
            canvas.StrokeColor = Colors.LightBlue;
            canvas.StrokeSize = _arcWidth;
            canvas.DrawArc(arcLeft, arcTop, _outerRadius * 2, _outerRadius * 2, -90, 0, false, false);

            // 7. Fix: Calculate the radius of the slider's path according to the arc width
            float handleRadius = _innerRadius + _arcWidth;

            // 8. Calculate the position of the slider (along the middle path)
            float radian = (float)(Math.PI * (- 90 + _viewModel.Angle) / 180);
            float handleX = _centerX + handleRadius * (float)Math.Cos(radian);
            float handleY = _centerY - handleRadius * (float)Math.Sin(radian);

            // 9. Draw the small round slider nested inside the arc
            canvas.FillColor = Color.FromRgba(0, 204, 238, 1f);
            canvas.FillCircle(handleX, handleY, _handleRadius);

            // 10. Calculate the middle point of the arc
            //float midRadian = (float)(Math.PI * 0 / 180);
            //float midX = _centerX + handleRadius * (float)Math.Cos(midRadian);
            //float midY = _centerY - handleRadius * (float)Math.Sin(midRadian);

            // 11. Draw a square box to display the angle
            float boxSize = 50;
            float boxLeft = _centerX - boxSize / 2+25;
            float boxTop = _centerY;

            // Draw the box background
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(boxLeft, boxTop, boxSize, boxSize);

            // Draw the box border
            canvas.StrokeColor = Color.FromRgba(50, 50, 50, 0f);
            canvas.StrokeSize = 1;
            canvas.DrawRectangle(boxLeft, boxTop, boxSize, boxSize);

            // Display the angle text inside the box
            canvas.FontColor = Colors.Black;
            canvas.FontSize = 20;
            canvas.DrawString($"{_viewModel.Angle:0}бу",
                              boxLeft, boxTop, boxSize, boxSize,
                              HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
