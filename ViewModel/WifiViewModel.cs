using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using Camera.Model;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Client;
using MQTTnet.Client.Options;



namespace Camera.ViewModel
{
    public class WifiViewModel : INotifyPropertyChanged
    {
        private readonly WifiModel _wifiModel;
        private IMqttClient _mqttClient;
        private IMqttClientOptions _mqttOptions;

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        public bool IsConnected
        {
            get => _wifiModel.IsConnected;
            set
            {
                if (_wifiModel.IsConnected != value)
                {
                    _wifiModel.IsConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
        }

        public bool IsnotConnected
        {
            get => _wifiModel.IsnotConnected;
            set
            {
                if (_wifiModel.IsnotConnected != value)
                {
                    _wifiModel.IsnotConnected = value;
                    OnPropertyChanged(nameof(IsnotConnected));
                }
            }
        }

        public string SSID
        {
            get => _wifiModel.SSID;
            set
            {
                if (_wifiModel.SSID != value)
                {
                    _wifiModel.SSID = value;
                    OnPropertyChanged(nameof(SSID));
                }
            }
        }

        public string Password
        {
            get => _wifiModel.Password;
            set
            {
                if (_wifiModel.Password != value)
                {
                    _wifiModel.Password = value;
                    OnPropertyChanged(nameof(Password));
                }
            }
        }

        public ICommand ConnectToServerCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand SendWifiCredentialsCommand { get; private set; }

        public WifiViewModel()
        {
            _wifiModel = new WifiModel();

            ConnectToServerCommand = new Command(async () => await ConnectToMqttBrokerAsync());
            DisconnectCommand = new Command(async () => await DisconnectFromMqttBrokerAsync());
            SendWifiCredentialsCommand = new Command(async () => await SendWifiCredentialsAsync());
        }

        private async Task ConnectToMqttBrokerAsync()
        {
            StatusMessage = "Connecting to MQTT broker...";
            try
            {
                var factory = new MqttFactory();
                _mqttClient = factory.CreateMqttClient();

                _mqttClient.Connected += async (s, e) =>
                {
                    StatusMessage = "Connected to MQTT broker!";
                    IsConnected = true;
                    IsnotConnected = false;

                    // Subscribe to status topic
                    await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                        .WithTopic("wifi/status")
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                        .Build());
                };

                _mqttClient.Disconnected += (s, e) =>
                {
                    StatusMessage = "Disconnected from broker.";
                    IsConnected = false;
                    IsnotConnected = true;
                };

                _mqttClient.ApplicationMessageReceived += (s, e) =>
                {
                    var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    if (e.ApplicationMessage.Topic == "wifi/status")
                    {
                        StatusMessage = message == "success"
                            ? "WiFi connection successful!"
                            : "WiFi connection failed.";
                    }
                };

                _mqttOptions = new MqttClientOptionsBuilder()
                    .WithClientId("mobile-app")
                    .WithTcpServer(_wifiModel.ServerIP, _wifiModel.ServerPort)
                    .Build();

                await _mqttClient.ConnectAsync(_mqttOptions);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to connect: {ex.Message}";
            }
        }

        private async Task SendWifiCredentialsAsync()
        {
            if (string.IsNullOrWhiteSpace(SSID) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Please enter a valid WiFi name and password!";
                return;
            }

            try
            {
                var wifiConfig = new
                {
                    ssid = SSID,
                    password = Password
                };

                string payload = JsonSerializer.Serialize(wifiConfig);

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("wifi/config")
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                    .WithRetainFlag(false)
                    .Build();

                await _mqttClient.PublishAsync(message);
                StatusMessage = "WiFi credentials sent.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to send WiFi credentials: {ex.Message}";
            }
        }

        private async Task DisconnectFromMqttBrokerAsync()
        {
            try
            {
                if (_mqttClient?.IsConnected ?? false)
                {
                    await _mqttClient.DisconnectAsync();
                }

                IsConnected = false;
                IsnotConnected = true;
                StatusMessage = "Disconnected from server.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to disconnect: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


















/*using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;
using Camera.Model;

namespace Camera.ViewModel
{
    public class WifiViewModel : INotifyPropertyChanged
    {
        // Model to store all configuration information
        private readonly WifiModel _wifiModel;

        // TCP client and network stream
        private TcpClient _client;
        private NetworkStream _stream;

        // Status message displayed in the UI
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        // Connection status (true = connected, false = disconnected)
        public bool IsConnected
        {
            get => _wifiModel.IsConnected;
            set
            {
                if (_wifiModel.IsConnected != value)
                {
                    _wifiModel.IsConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
        }

        // Inverse of connection status, for UI binding
        public bool IsnotConnected
        {
            get => _wifiModel.IsnotConnected;
            set
            {
                if (_wifiModel.IsnotConnected != value)
                {
                    _wifiModel.IsnotConnected = value;
                    OnPropertyChanged(nameof(IsnotConnected));
                }
            }
        }

        // WiFi SSID (network name)
        public string SSID
        {
            get => _wifiModel.SSID;
            set
            {
                if (_wifiModel.SSID != value)
                {
                    _wifiModel.SSID = value;
                    OnPropertyChanged(nameof(SSID));
                }
            }
        }

        // WiFi password
        public string Password
        {
            get => _wifiModel.Password;
            set
            {
                if (_wifiModel.Password != value)
                {
                    _wifiModel.Password = value;
                    OnPropertyChanged(nameof(Password));
                }
            }
        }

        // Commands for UI actions
        public ICommand ConnectToServerCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand SendWifiCredentialsCommand { get; private set; }

        // Constructor
        public WifiViewModel()
        {
            // Initialize Model
            _wifiModel = new WifiModel();

            // Initialize Commands
            ConnectToServerCommand = new Command(async () => await ConnectToServerAsync());
            DisconnectCommand = new Command(DisconnectFromServer);
            SendWifiCredentialsCommand = new Command(async () => await SendWiFiCredentialsAsync());
        }

        // Connect to the server
        private async Task ConnectToServerAsync()
        {
            StatusMessage = "Connecting to the server...";
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_wifiModel.ServerIP, _wifiModel.ServerPort);
                _stream = _client.GetStream();

                // Update connection status
                IsConnected = true;
                IsnotConnected = false;

                StatusMessage = "Server connection successful!";
            }
            catch (Exception ex)
            {
                // Handle connection failure
                IsConnected = false;
                IsnotConnected = true;
                StatusMessage = $"Failed to connect: {ex.Message}";
            }
        }

        // Send WiFi credentials to the server
        private async Task SendWiFiCredentialsAsync()
        {
            // Validate SSID and Password
            if (string.IsNullOrWhiteSpace(SSID) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Please enter a valid WiFi name and password!";
                return;
            }

            try
            {
                byte[] buffer = new byte[1024];
                byte[] ssidBytes = Encoding.UTF8.GetBytes(SSID + "\n");
                await _stream.WriteAsync(ssidBytes, 0, ssidBytes.Length);

                byte[] passwordBytes = Encoding.UTF8.GetBytes(Password + "\n");
                await _stream.WriteAsync(passwordBytes, 0, passwordBytes.Length);

                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                string serverResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                // Update status message based on server response
                StatusMessage = serverResponse == "Success" ? "WiFi connection successful!" : "WiFi connection failed.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to send WiFi credentials: {ex.Message}";
            }
        }

        // Disconnect from the server
        private void DisconnectFromServer()
        {
            try
            {
                _stream?.Close();
                _client?.Close();
                IsConnected = false;
                IsnotConnected = true;
                StatusMessage = "Disconnected from server.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to disconnect: {ex.Message}";
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
*/