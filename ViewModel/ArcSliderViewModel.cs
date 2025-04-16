using System.ComponentModel;
using System.Runtime.CompilerServices;
using Camera.Model;
using Camera.Drawables;
using Camera.Datebase;
using MySqlConnector;
using MQTTnet.Client.Options;
using MQTTnet.Client;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Client.Connecting;
using System.Text.Json;

namespace Camera.ViewModel
{
    public class ArcSliderViewModel : INotifyPropertyChanged
    {
        private readonly WifiModel _wifiModel;
        // MQTT client and its connection options
        private IMqttClient _mqttClient;
        private IMqttClientOptions _mqttOptions;

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

                    //Update angle with mqtt (make sure SelectedCamera is not null)
                    
                    _= UpdateAngleInPi();
                   
                }
            }
        }

        private async Task UpdateAngleInPi()
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
            {
                var factory = new MqttFactory();
                _mqttClient = factory.CreateMqttClient();

                _mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(async e =>
                {
                    Console.WriteLine("Connected to MQTT Broker");
                });

                _mqttOptions = new MqttClientOptionsBuilder()
                    .WithClientId("camera_control_app")
                    .WithTcpServer(_wifiModel.ServerIP, _wifiModel.ServerPort)
                    .Build();

                await _mqttClient.ConnectAsync(_mqttOptions);
            }

            var panPayload = new
            {
                pan = Angle
            };

            string payload = JsonSerializer.Serialize(panPayload);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("cam/set/pan") // 
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.PublishAsync(message);
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
            _wifiModel = new WifiModel();
            _model = new ArcSliderModel();
            ArcSliderDrawable = new ArcSliderDrawable(this);

            // Get CamerasDashboardViewModel via Service
            _cameraDashboardViewModel = MauiProgram.Services.GetService<CameraDashboardViewModel>();

            _databaseService = new DatebaseService();

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
