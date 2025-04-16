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


        private bool _isStreaming = false;
        public bool IsStreaming
        {
            get => _isStreaming;
            set
            {
                if (_isStreaming != value)
                {
                    _isStreaming = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsStreamingText)); 
                }
            }
        }

        public string IsStreamingText => IsStreaming ? "Stop Stream" : "Start Stream";

        public ICommand ToggleStreamingCommand { get; }

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


            ToggleStreamingCommand = new Command(async () => await ToggleStreamAsync());
            _ = SubscribeToStatusAsync();

        }

        private async Task ToggleStreamAsync()
        {
            IsStreaming = !IsStreaming; // state switching

            var factory = new MqttFactory();
            _pubmqttClient = factory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId("camera_control_app_pub")
                .WithTcpServer(_wifiModel.ServerIP, _wifiModel.ServerPort)
                .Build();

            
                await _pubmqttClient.ConnectAsync(_mqttOptions);

                var payload = JsonSerializer.Serialize(new { is_streaming = IsStreaming });

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("cam/set/stream")
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



                        if (json.TryGetProperty("is_streaming", out var isStreamingProp))
                        {
                            bool value = isStreamingProp.GetBoolean();
                            MainThread.BeginInvokeOnMainThread(() => IsStreaming = value);
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
