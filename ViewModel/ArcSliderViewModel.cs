using System.ComponentModel;
using System.Runtime.CompilerServices;
using Camera.Model;
using Camera.Drawables;
using Camera.Datebase;
using MySqlConnector;

namespace Camera.ViewModel
{
    public class ArcSliderViewModel : INotifyPropertyChanged
    {
        // Model instance to store state
        private readonly ArcSliderModel _model;
        private readonly DatebaseService _databaseService;
        private readonly CameraDashboardViewModel _cameraDashboardViewModel;

        private int _selectedCameraId;
        public int SelectedCameraId
        {
            get => _selectedCameraId;
            set
            {
                if (_selectedCameraId != value)
                {
                    _selectedCameraId = value;
                    OnPropertyChanged();
                }
            }
        }

        // Public properties exposed to View and Drawable
        public float Angle
        {
            get => _model.Angle;
            set
            {
                if (_model.Angle != value)
                {
                    _model.Angle = NormalizeAngle(value);
                    OnPropertyChanged();
                    RequestRedraw?.Invoke();

                    //Update database (make sure SelectedCamera is not null)
                    if (_cameraDashboardViewModel.SelectedCamera != null)
                    {
                        UpdateAngleInDatabase(_cameraDashboardViewModel.SelectedCamera.Id);
                    }
                }
            }
        }

        private void UpdateAngleInDatabase(int cameraId)
        {
            if (cameraId == 0) return; // Avoid invalid IDs

            try
            {
                using (var connection = _databaseService.GetConnection())
                {
                    connection.Open();

                    string query = "UPDATE cameras SET tilt_angle = @Angle WHERE id = @CameraId";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Angle", Angle);
                        command.Parameters.AddWithValue("@CameraId", cameraId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Console.WriteLine($"✅ Camera {cameraId} angle updated successfully.");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ No record updated. Check the camera ID.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database update failed: {ex.Message}");
            }
        }

        // Input text for the angle entry
        public string InputText
        {
            get => _model.InputText;
            set
            {
                _model.InputText = value;
                OnPropertyChanged();
            }
        }

        // Visibility for the input entry
        public bool IsEntryVisible
        {
            get => _model.IsEntryVisible;
            set
            {
                _model.IsEntryVisible = value;
                OnPropertyChanged();
            }
        }

        // Drawable instance for rendering the slider
        public ArcSliderDrawable ArcSliderDrawable { get; set; }
        public Action RequestRedraw;

        // Constructor to initialize the model and drawable
        public ArcSliderViewModel()
        {
            _model = new ArcSliderModel();
            ArcSliderDrawable = new ArcSliderDrawable(this);

            // Get CamerasViewModel via Service
            _cameraDashboardViewModel = MauiProgram.Services.GetService<CameraDashboardViewModel>();

            _databaseService = new DatebaseService("server=192.168.31.151;port=3306;database=traffic_analysis;user=root;password=123456;");

            //  Listen for CamerasViewModel.SelectedCamera changes.
            if (_cameraDashboardViewModel != null)
            {
                _cameraDashboardViewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(CamerasViewModel.SelectedCamera))
                    {
                        UpdateSelectedCamera();
                    }
                };

                // Initialise SelectedCameraId
                UpdateSelectedCamera();
            }

            // Initialize state
            IsEntryVisible = false;
            InputText = $"{Angle}";
        }


        // Update `SelectedCameraId' when `SelectedCamera` changes.
        private void UpdateSelectedCamera()
        {
            if (_cameraDashboardViewModel.SelectedCamera != null)
            {
                SelectedCameraId = _cameraDashboardViewModel.SelectedCamera.Id;
            }
        }


        // Helper method to normalize and constrain the angle
        private float NormalizeAngle(float value)
        {
            if (value > 90) return 90;
            if (value < -90) return -90;
            return value;
        }

        // Check if the touch is within the arc's bounds
        private bool IsInsideArc(float touchX, float touchY)
        {
            float deltaX = touchX - ArcSliderDrawable._centerX;
            float deltaY = touchY - ArcSliderDrawable._centerY;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            bool isInsideRadius = distance >= ArcSliderDrawable._innerRadius - 20 &&
                                  distance <= ArcSliderDrawable._outerRadius + 40;

            return isInsideRadius;
        }

        // Update the angle based on touch input
        public void UpdateAngle(float touchX, float touchY, float centerX, float centerY)
        {
            if (!IsInsideArc(touchX, touchY))
                return;

            float deltaX = centerX - touchX;
            float deltaY = centerY - touchY;

            float radians = (float)Math.Atan2(deltaY, deltaX);
            float degree = radians * 180 / (float)Math.PI;

            float newAngle = degree - 90;
            float deltaAngle = newAngle - Angle;

            if (Math.Abs(deltaAngle) > 180)
            {
                newAngle += deltaAngle > 0 ? -360 : 360;
            }

            Angle = NormalizeAngle(newAngle);
        }

        // Toggle input entry visibility
        public void ShowEntry()
        {
            IsEntryVisible = true;
            InputText = $"{Angle:0}";
        }

        // Confirm input and update angle
        public void ConfirmInput()
        {
            if (float.TryParse(InputText, out float newAngle))
            {
                Angle = NormalizeAngle(newAngle);
                RequestRedraw?.Invoke();
            }

            IsEntryVisible = false;
        }

        // Check if touch is inside the input box
        public bool HandleTouch(float touchX, float touchY)
        {
            float boxSize = 50;
            float boxLeft = ArcSliderDrawable._centerX - boxSize / 2;
            float boxTop = ArcSliderDrawable._centerY - 50;

            return touchX >= boxLeft && touchX <= boxLeft + boxSize &&
                   touchY >= boxTop && touchY <= boxTop + boxSize;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
