using System.ComponentModel;
using System.Runtime.CompilerServices;
using Camera.Model;
using Camera.Drawables;
using Camera.Datebase;
using MySqlConnector;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using MQTTnet;
using System.Text.Json;
using MQTTnet.Client;
using System.Diagnostics;
using static Camera.ViewModel.ArcSliderViewModel;


namespace Camera.ViewModel
{
    public class ArcSlider_VerViewModel : INotifyPropertyChanged
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
        public int Angle
        {
            get => _model.Angle_ver;
            set
            {
                if (_model.Angle_ver != value)
                {
                    _model.Angle_ver = NormalizeAngle(value);
                    OnPropertyChanged();
                    RequestRedraw?.Invoke();

                    //Update angle with mqtt (make sure SelectedCamera is not null)

                    _ = UpdateAngleInPi();

                }
            }
        }

        public int MinAngle
        {
            get => _model.MinAngle_ver;
            set
            {
                if (_model.MinAngle_ver != value)
                {
                    _model.MinAngle_ver = NormalizeAngle(value);

                    // Ensure that the main angle is not out of the new range
                    if (Angle < _model.MinAngle_ver)
                        Angle = _model.MinAngle_ver;

                    OnPropertyChanged();
                    RequestRedraw?.Invoke();

                    _ = UpdateRangeInPi();
                }
            }
        }

        public int MaxAngle
        {
            get => _model.MaxAngle_ver;
            set
            {
                if (_model.MaxAngle_ver != value)
                {
                    _model.MaxAngle_ver = NormalizeAngle(value);

                    // Ensure that the main angle is not out of the new range
                    if (Angle > _model.MaxAngle_ver)
                        Angle = _model.MaxAngle_ver;

                    OnPropertyChanged();
                    RequestRedraw?.Invoke();

                    _ = UpdateRangeInPi();
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

            var payload = Angle.ToString();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("cam/set/tilt") // 
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.PublishAsync(message);
        }

        private async Task UpdateRangeInPi()
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

            var payload = $"{MinAngle},{MaxAngle}";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("cam/set/tilt_range") // 
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
        public ArcSlider_VerDrawable ArcSlider_VerDrawable { get; set; }
        public Action RequestRedraw;

        // Constructor to initialize the model and drawable
        public ArcSlider_VerViewModel()
        {
            
            _wifiModel = new WifiModel();
            _model = new ArcSliderModel();
            ArcSlider_VerDrawable = new ArcSlider_VerDrawable(this);

            // Get CamerasDashboardViewModel via Service
            _cameraDashboardViewModel = MauiProgram.Services.GetService<CameraDashboardViewModel>();

            _databaseService = new DatebaseService();

            // Listen for CamerasViewModel.SelectedCamera changes.
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

        //  Update `SelectedCameraId` when `SelectedCamera` changes.
        private void UpdateSelectedCamera()
        {
            if (_cameraDashboardViewModel.SelectedCamera != null)
            {
                SelectedCameraId = _cameraDashboardViewModel.SelectedCamera.Id;
            }
        }

        // Helper method to normalize and constrain the angle
        private int NormalizeAngle(int value)
        {
            if (value > 90) return 90;
            if (value < 0) return 0;
            return value;
        }

        // Check if the touch is within the arc's bounds
        private bool IsInsideArc(float touchX, float touchY)
        {
            float deltaX = touchX - ArcSlider_VerDrawable._centerX;
            float deltaY = touchY - ArcSlider_VerDrawable._centerY;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            bool isInsideRadius = distance >= ArcSlider_VerDrawable._innerRadius - 20 &&
                                  distance <= ArcSlider_VerDrawable._outerRadius + 40;

            return isInsideRadius;
        }

        public enum HandleType { None, Main, Min, Max }
        public HandleType ActiveHandle { get; set; } = HandleType.None; //the signal of selected ball

        //check click which ball and selecte it
        public void StartHandleSelection(float touchX, float touchY, float centerX, float centerY)
        {
            float handleRadius = ArcSlider_VerDrawable._innerRadius + ArcSlider_VerDrawable._arcWidth;
            float touchThreshold = ArcSlider_VerDrawable._handleRadius * 1.5f;

            var mainPos = CalculateHandlePosition(Angle, centerX, centerY, handleRadius);
            var minPos = CalculateHandlePosition(MinAngle, centerX, centerY, handleRadius);
            var maxPos = CalculateHandlePosition(MaxAngle, centerX, centerY, handleRadius);

            ActiveHandle = HandleType.None;

            if (IsPointInCircle(touchX, touchY, mainPos.X, mainPos.Y, touchThreshold))
                ActiveHandle = HandleType.Main;
            else if (IsPointInCircle(touchX, touchY, minPos.X, minPos.Y, touchThreshold))
                ActiveHandle = HandleType.Min;
            else if (IsPointInCircle(touchX, touchY, maxPos.X, maxPos.Y, touchThreshold))
                ActiveHandle = HandleType.Max;
            else
                ActiveHandle = HandleType.Main;

        }

        public void EndHandleSelection()
        {
            ActiveHandle = HandleType.None;
        }

        // Update the angle based on touch input
        public void UpdateAngle(float touchX, float touchY, float centerX, float centerY)
        {
            /*if (!IsInsideArc(touchX, touchY))
                return;*/

            float compensatedY = touchY - 30;

            float deltaX = centerX - touchX;
            float deltaY = centerY + compensatedY;

            float radians = (float)Math.Atan2(deltaY, deltaX);
            float degree = radians * 180 / (float)Math.PI;

            int newAngle = (int)degree - 90;
            int deltaAngle = newAngle - Angle;

            if (Math.Abs(deltaAngle) > 180)
            {
                newAngle += deltaAngle > 0 ? -360 : 360;
            }

            newAngle = NormalizeAngle(newAngle);

            //System.Diagnostics.Debug.WriteLine($"Touch: ({touchX:F1},{touchY:F1}) Center: ({centerX:F1},{centerY:F1})");
            //System.Diagnostics.Debug.WriteLine($"Delta: ({deltaX:F1},{deltaY:F1})");
            //System.Diagnostics.Debug.WriteLine($"Radians: {radians:F2} Degree: {degree:F1} RawAngle: {newAngle}");
            //System.Diagnostics.Debug.WriteLine($"Draw CenterY: {ArcSlider_VerDrawable._centerY} | TouchY: {touchY}");

            // Slider recognition
            switch (ActiveHandle)
            {
                case HandleType.Main:
                    if (newAngle <= MaxAngle && newAngle >= MinAngle)
                        Angle = newAngle;
                    break;
                case HandleType.Min:
                    if (newAngle <= Angle)
                        MinAngle = newAngle;
                    break;
                case HandleType.Max:
                    if (newAngle >= Angle)
                        MaxAngle = newAngle;
                    break;
            }

        }

        // calculate slider position
        private (float X, float Y) CalculateHandlePosition(float angle, float centerX, float centerY, float radius)
        {
            float radian = (float)(Math.PI * (-90 + angle) / 180);
            return (
                centerX + radius * (float)Math.Cos(radian),
                centerY - radius * (float)Math.Sin(radian) 
            );
        }

        private bool IsPointInCircle(float x1, float y1, float x2, float y2, float radius)
        {
            float dx = x1 - x2;
            float dy = y1 - y2;
            return (dx * dx + dy * dy) <= (radius * radius);
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
            if (int.TryParse(InputText, out int newAngle))
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
            float boxLeft = ArcSlider_VerDrawable._centerX - boxSize / 2+50;
            float boxTop = ArcSlider_VerDrawable._centerY;

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
