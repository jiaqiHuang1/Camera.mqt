using MQTTnet.Client.Options;
using MQTTnet;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using MQTTnet.Client;
using Camera.Model;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Receiving;
using System.Text;

namespace Camera.ViewModel
{
    public class TwoArcSlider : INotifyPropertyChanged
    {
        private readonly WifiModel _wifiModel;
        // MQTT client and its connection options
        private IMqttClient _submqttClient;
        private IMqttClient _pubmqttClient;
        private IMqttClientOptions _mqttOptions;

        private readonly CameraDashboardViewModel _cameraDashboardViewModel;
        // Two independent slider ViewModels
        private ArcSliderViewModel _sliderViewModel;
        public ArcSliderViewModel SliderViewModel
        {
            get => _sliderViewModel;
            set
            {
                if (_sliderViewModel != value)
                {
                    _sliderViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        private ArcSlider_VerViewModel _slider_VerViewModel;
        public ArcSlider_VerViewModel Slider_VerViewModel
        {
            get => _slider_VerViewModel;
            set
            {
                if (_slider_VerViewModel != value)
                {
                    _slider_VerViewModel = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isVideoStreamVisible = false;

        public bool IsVideoStreamVisible
        {
            get => _isVideoStreamVisible;
            set
            {
                if (_isVideoStreamVisible != value)
                {
                    _isVideoStreamVisible = value;
                    OnPropertyChanged();

                    // update VideoRowHeight
                    VideoRowHeight = value ? new GridLength(400) : new GridLength(0);
                }
            }
        }

        private GridLength _videoRowHeight = new GridLength(0);
        public GridLength VideoRowHeight
        {
            get => _videoRowHeight;
            set
            {
                if (_videoRowHeight != value)
                {
                    _videoRowHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _selectedSegmentIndex;
        public int SelectedSegmentIndex
        {
            get => _selectedSegmentIndex;
            set
            {
                if (_selectedSegmentIndex != value)
                {
                    _selectedSegmentIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPanSelected));
                    OnPropertyChanged(nameof(IsTiltSelected));
                }
            }
        }

        public bool IsPanSelected => SelectedSegmentIndex == 0;
        public bool IsTiltSelected => SelectedSegmentIndex == 1;

        // WebView source
        private string _webViewSource;
        public string WebViewSource
        {
            get => _webViewSource;
            set
            {
                if (_webViewSource != value)
                {
                    _webViewSource = value;
                    OnPropertyChanged();
                }
            }
        }


        private bool _isAutomatic_pan = false;
        public bool IsAutomatic_pan
        {
            get => _isAutomatic_pan;
            set
            {
                if (_isAutomatic_pan != value)
                {
                    _isAutomatic_pan = value;
                    OnPropertyChanged();
                    _ = ToggleAutomatic_panAsync();
                }
            }
        }

        private bool _isAutomatic_tilt = false;
        public bool IsAutomatic_tilt
        {
            get => _isAutomatic_tilt;
            set
            {
                if (_isAutomatic_tilt != value)
                {
                    _isAutomatic_tilt = value;
                    OnPropertyChanged();
                    _ = ToggleAutomatic_tiltAsync();
                }
            }
        }

        private int _panSpeedValue;
        public int Pan_SpeedValue
        {
            get => _panSpeedValue;
            set
            {
                if (_panSpeedValue != value)
                {
                    _panSpeedValue = value;
                    OnPropertyChanged();
                    _ = HandlePanSpeedChanged(value);
                }
            }
        }

        private int _tiltSpeedValue;
        public int Tilt_SpeedValue
        {
            get => _tiltSpeedValue;
            set
            {
                if (_tiltSpeedValue != value)
                {
                    _tiltSpeedValue = value;
                    OnPropertyChanged();
                    _ = HandleTiltSpeedChanged(value);
                }
            }
        }

        private async Task HandlePanSpeedChanged(int newValue)
        {
            var factory = new MqttFactory();
            _pubmqttClient = factory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("camera_control_app_pub")
                .WithTcpServer(_wifiModel.ServerIP, _wifiModel.ServerPort)
                .Build();


            await _pubmqttClient.ConnectAsync(_mqttOptions);

            var payload = Pan_SpeedValue.ToString();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("cam/set/speed/pan")
                .WithPayload(payload)
                .WithExactlyOnceQoS()
                .WithRetainFlag(false)
                .Build();
            await _pubmqttClient.PublishAsync(message);
        }


        private async Task HandleTiltSpeedChanged(int newValue)
        {
            var factory = new MqttFactory();
            _pubmqttClient = factory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("camera_control_app_pub")
                .WithTcpServer(_wifiModel.ServerIP, _wifiModel.ServerPort)
                .Build();


            await _pubmqttClient.ConnectAsync(_mqttOptions);

            var payload = Tilt_SpeedValue.ToString();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("cam/set/speed/tilt")
                .WithPayload(payload)
                .WithExactlyOnceQoS()
                .WithRetainFlag(false)
                .Build();
            await _pubmqttClient.PublishAsync(message);
        }


        // Constructor: Initialize two ViewModel instances
        public TwoArcSlider()
        {
            _wifiModel = new WifiModel();
            SliderViewModel = new ArcSliderViewModel();
            Slider_VerViewModel = new ArcSlider_VerViewModel();

            // **Get from Service Locator CamerasViewModel**
            _cameraDashboardViewModel = MauiProgram.Services.GetService<CameraDashboardViewModel>();


            if (_cameraDashboardViewModel != null)
            {
                // **Listening for CamerasViewModel.WebViewSource changes**
                _cameraDashboardViewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(CameraDashboardViewModel.WebViewSource))
                    {
                        UpdateWebViewSource();
                    }
                };

                // **initialisation WebViewSource**
                UpdateWebViewSource();
            }

            // Subscribe to redraw requests
            SliderViewModel.RequestRedraw += () => RequestRedraw?.Invoke();
            Slider_VerViewModel.RequestRedraw += () => RequestRedraw?.Invoke();

            _ = SubscribeToStatusAsync();

        }

        private async Task ToggleAutomatic_panAsync()
        {

            var factory = new MqttFactory();
            _pubmqttClient = factory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("camera_control_app_pub")
                .WithTcpServer(_wifiModel.ServerIP, _wifiModel.ServerPort)
                .Build();

            
                await _pubmqttClient.ConnectAsync(_mqttOptions);

                var payload = JsonSerializer.Serialize("");
                string topic = IsAutomatic_pan ? "cam/set/pan/automatic" : "cam/set/pan/manual";

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag(false)
                    .Build();

                await _pubmqttClient.PublishAsync(message);

        }

        private async Task ToggleAutomatic_tiltAsync()
        {

            var factory = new MqttFactory();
            _pubmqttClient = factory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("camera_control_app_pub")
                .WithTcpServer(_wifiModel.ServerIP, _wifiModel.ServerPort)
                .Build();


            await _pubmqttClient.ConnectAsync(_mqttOptions);

            var payload = JsonSerializer.Serialize("");
            string topic = IsAutomatic_tilt ? "cam/set/tilt/automatic" : "cam/set/tilt/manual";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithExactlyOnceQoS()
                .WithRetainFlag(false)
                .Build();

            await _pubmqttClient.PublishAsync(message);
            

        }

            public async Task SubscribeToStatusAsync()
        {
            System.Diagnostics.Debug.WriteLine("SubscribeToStatusAsync() called");
            var factory = new MqttFactory();
            _submqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithClientId("camera_control_app_sub")
                .WithTcpServer(_wifiModel.ServerIP, _wifiModel.ServerPort)
                .Build();

            _submqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
            {
                try
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    var topic = e.ApplicationMessage.Topic;
                    System.Diagnostics.Debug.WriteLine($"[MQTT] Received message on topic `{topic}`: {payload}");

                    if (topic == "cam/get/status")
                    {
                        var json = JsonDocument.Parse(payload).RootElement;



                        if (json.TryGetProperty("mode_manual", out var isStreamingProp))
                        {
                            bool value = isStreamingProp.GetBoolean();
                            MainThread.BeginInvokeOnMainThread(() => IsAutomatic_pan = value);
                        }
                        if (json.TryGetProperty("tilt", out var tiltProp))
                        {
                            int value = tiltProp.GetInt32();
                            MainThread.BeginInvokeOnMainThread(() => _slider_VerViewModel.Angle = value);
                            System.Diagnostics.Debug.WriteLine($"[UI] Vertical Angle set to: {_slider_VerViewModel.Angle}");
                        }
                        if (json.TryGetProperty("pan", out var panProp))
                        {
                            int value = panProp.GetInt32();
                            MainThread.BeginInvokeOnMainThread(() => _sliderViewModel.Angle = value);
                            System.Diagnostics.Debug.WriteLine($"[UI] Horizontal Angle set to: {_sliderViewModel.Angle}");
                        }
                        if (json.TryGetProperty("speed_pan", out var speed_panProp))
                        {
                            int value = speed_panProp.GetInt32();
                            MainThread.BeginInvokeOnMainThread(() => Pan_SpeedValue = value);
                            System.Diagnostics.Debug.WriteLine($"[UI] Pan speed set to: {Pan_SpeedValue}");
                        }
                        if (json.TryGetProperty("speed_tilt", out var speed_tiltProp))
                        {
                            int value = speed_tiltProp.GetInt32();
                            MainThread.BeginInvokeOnMainThread(() => Tilt_SpeedValue = value);
                            System.Diagnostics.Debug.WriteLine($"[UI] Tilt speed set to: {Tilt_SpeedValue}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MQTT] Error parsing status message: {ex.Message}");
                }
            });

            _submqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(async e =>
            {
                await _submqttClient.SubscribeAsync("cam/get/status");
            });

            await _submqttClient.ConnectAsync(options);
        }


        // **update WebViewSource**
        private void UpdateWebViewSource()
        {
            WebViewSource = _cameraDashboardViewModel?.WebViewSource;
        }

        // Event to request redraw for the entire view
        public event Action RequestRedraw;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
