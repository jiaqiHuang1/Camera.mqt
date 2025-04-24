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
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

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

        private int _selectedIndex;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPanSelected));
                    OnPropertyChanged(nameof(IsTiltSelected));
                    UpdateButtonColors();
                }
            }
        }

        private Color _panButtonColor;
        public Color PanButtonColor
        {
            get => _panButtonColor;
            set { _panButtonColor = value; OnPropertyChanged(); }
        }

        private Color _tiltButtonColor;
        public Color TiltButtonColor
        {
            get => _tiltButtonColor;
            set { _tiltButtonColor = value; OnPropertyChanged(); }
        }

        public bool IsPanSelected => SelectedIndex == 0;
        public bool IsTiltSelected => SelectedIndex == 1;

        public ICommand SelectSegmentCommand { get; }

        private void OnSelectSegment(string param)
        {
            if (int.TryParse(param, out int index))
            {
                SelectedIndex = index;
            }
        }

        private void UpdateButtonColors()
        {
            PanButtonColor = (SelectedIndex == 0) ? Colors.DodgerBlue : Colors.Gray;
            TiltButtonColor = (SelectedIndex == 1) ? Colors.DodgerBlue : Colors.Gray;
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

                    CheckPollingStatus();
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

                    CheckPollingStatus();
                }
            }
        }

        private System.Timers.Timer _statusPollingTimer;

        private void StartAutoStatusPolling()
        {
            _statusPollingTimer?.Stop();

            _statusPollingTimer = new System.Timers.Timer(100); // 0.1s
            _statusPollingTimer.Elapsed += async (s, e) =>
            {
                if (IsAutomatic_pan || IsAutomatic_tilt)
                {
                    await SubscribeToStatusAsync(); // 
                }
            };
            _statusPollingTimer.AutoReset = true;
            _statusPollingTimer.Start();
        }

        private void CheckPollingStatus()
        {
            if (IsAutomatic_pan || IsAutomatic_tilt)
            {
                StartAutoStatusPolling(); 
            }
            else
            {
                _statusPollingTimer?.Stop(); 
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

            SelectSegmentCommand = new Command<string>(OnSelectSegment);
            SelectedIndex = 0; // default Pan
            UpdateButtonColors();

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

            _allowRemoteAutoStateUpdate = false; // Disable MQTT writeback

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


            /*MainThread.BeginInvokeOnMainThread(() =>
            {
                System.Timers.Timer delayTimer = new System.Timers.Timer(1000); // 1s
                delayTimer.Elapsed += (s, e) =>
                {
                    delayTimer.Stop();
                    delayTimer.Dispose();
                    _allowRemoteAutoStateUpdate = true;
                };
                delayTimer.Start();
            });*/
        }

        private async Task ToggleAutomatic_tiltAsync()
        {
            _allowRemoteAutoStateUpdate = false; // Disable MQTT writeback
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

            /*MainThread.BeginInvokeOnMainThread(() =>
            {
                System.Timers.Timer delayTimer = new System.Timers.Timer(1000); // 1s
                delayTimer.Elapsed += (s, e) =>
                {
                    delayTimer.Stop();
                    delayTimer.Dispose();
                    _allowRemoteAutoStateUpdate = true;
                };
                delayTimer.Start();
            });*/
        }

        private bool _allowRemoteAutoStateUpdate = true;
        private bool _hasConnectedAndSubscribed = false;
        public async Task SubscribeToStatusAsync()
            {
            System.Diagnostics.Debug.WriteLine("SubscribeToStatusAsync() called");

            if (_hasConnectedAndSubscribed)
            {
                // if already connected and subscribed, push directly
                if (_submqttClient != null && _submqttClient.IsConnected)
                {
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("cam/get/status")
                        .WithPayload("")
                        .WithAtLeastOnceQoS()
                        .WithRetainFlag(false)
                        .Build();

                    await _submqttClient.PublishAsync(message);
                    System.Diagnostics.Debug.WriteLine("[MQTT] Published cam/get/status");
                }
                return;
            }

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

                    if (topic == "cam/status")
                    {
                        var json = JsonDocument.Parse(payload).RootElement;

                        if (json.TryGetProperty("pan", out var panProp))
                        {
                            if (panProp.TryGetProperty("angle", out var panAngleProp))
                            {
                                int panAngle = panAngleProp.GetInt32();   
                                MainThread.BeginInvokeOnMainThread(() => _sliderViewModel.Angle = panAngle);
                                
                            }

                            if (panProp.TryGetProperty("speed", out var panSpeedProp))
                            {
                                int panSpeed = panSpeedProp.GetInt32();

                                if (_allowRemoteAutoStateUpdate)
                                {
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        Pan_SpeedValue = panSpeed;
                                    });
                                }
                                
                            }

                            if (panProp.TryGetProperty("mode", out var panModeProp))
                            {
                                string mode = panModeProp.GetString()?.ToLower();
                                if (_allowRemoteAutoStateUpdate)
                                {
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        IsAutomatic_pan = mode == "automatic";
                                    });
                                }
                            }
                        }

                        if (json.TryGetProperty("tilt", out var tiltProp))
                        {
                            if (tiltProp.TryGetProperty("angle", out var tiltAngleProp))
                            {
                                int tiltAngle = tiltAngleProp.GetInt32();
                                MainThread.BeginInvokeOnMainThread(() => _slider_VerViewModel.Angle = tiltAngle);
                            }

                            if (tiltProp.TryGetProperty("speed", out var tiltSpeedProp))
                            {
                                int tiltSpeed = tiltSpeedProp.GetInt32();
                                MainThread.BeginInvokeOnMainThread(() => Tilt_SpeedValue = tiltSpeed);
                            }

                            if (tiltProp.TryGetProperty("mode", out var tiltModeProp))
                            {
                                string mode = tiltModeProp.GetString()?.ToLower();
                                if (_allowRemoteAutoStateUpdate)
                                {
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        IsAutomatic_tilt = mode == "automatic";
                                    });
                                }
                            }
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
                System.Diagnostics.Debug.WriteLine("MQTT connected. Subscribing to cam/status...");
                await _submqttClient.SubscribeAsync("cam/status");
                _hasConnectedAndSubscribed = true;

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("cam/get/status")
                    .WithPayload("")
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithRetainFlag(false)
                    .Build();
                await _submqttClient.PublishAsync(message);
                System.Diagnostics.Debug.WriteLine("Published cam/get/status to request status.");
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
