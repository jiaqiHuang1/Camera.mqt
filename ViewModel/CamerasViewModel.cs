using Camera.Model;
using Camera.Datebase;
using MySqlConnector;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;


namespace Camera.ViewModel
{
    public class CamerasViewModel : INotifyPropertyChanged
    {
        private readonly DatebaseService _databaseService;

        // Map icon button command
        public ICommand MapCommand { get; private set; }

        // Command to load cameras
        public ICommand LoadCamerasCommand { get; }

        // Command to toggle between map and list view
        public ICommand ToggleMapCommand { get; private set; }

        public ICommand ConnectionCommand { get; private set; }

        public CamerasViewModel()
        {
            // Manually create DatabaseService in the constructor
            var connectionString = "server=192.168.31.151;port=3306;database=traffic_analysis;user=root;password=123456;";
            _databaseService = new DatebaseService(connectionString);

            // Initialize camera list
            Cameras = new ObservableCollection<CameraInfo>();
            FilteredCameras = new ObservableCollection<CameraInfo>();

            // Initialize commands
            LoadCamerasCommand = new Command(LoadCameras);
            ToggleMapCommand = new Command(ToggleMapView);
            ConnectionCommand = new Command(OnConnectionButtonClicked);
        }

        // Load camera information
        public void LoadCameras()
        {
            Cameras.Clear();
            FilteredCameras.Clear();
            StatusMessage = "Loading camera information...";

            try
            {
                using (var connection = _databaseService.GetConnection())
                {
                    connection.Open();
                    var query = "SELECT * FROM cameras";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var cameraInfo = new CameraInfo
                                {
                                    Id = reader.GetInt32("id"),
                                    City = reader.GetString("city"),
                                    Street = reader.GetString("street"),
                                    UpdatedAt = reader.GetDateTime("updated_at"),
                                    IpAddress = reader.GetString("ip_address"),
                                    Port = reader.GetString("port"),
                                    PositionLat = reader.GetString("position_lat"),
                                    PositionLong = reader.GetString("position_long"),
                                    TiltAngle = reader.GetDouble("tilt_angle"),
                                    PanAngle = reader.GetDouble("pan_angle"),
                                };
                                Cameras.Add(cameraInfo);
                                FilteredCameras.Add(cameraInfo);
                            }
                        }
                    }
                }

                StatusMessage = "Camera information loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load: {ex.Message}";
            }
        }

        // Camera list
        private ObservableCollection<CameraInfo> _cameras;
        public ObservableCollection<CameraInfo> Cameras
        {
            get => _cameras;
            set
            {
                if (_cameras != value)
                {
                    _cameras = value;
                    OnPropertyChanged(nameof(Cameras));
                    UpdateFilteredCameras(); // Update filtered data
                }
            }
        }

        // Filtered camera data
        private ObservableCollection<CameraInfo> _filteredCameras;
        public ObservableCollection<CameraInfo> FilteredCameras
        {
            get => _filteredCameras;
            set
            {
                if (_filteredCameras != value)
                {
                    _filteredCameras = value;
                    OnPropertyChanged(nameof(FilteredCameras));

                    // Trigger map update event
                    UpdateMapRequested?.Invoke(this, FilteredCameras);
                }
            }
        }

        // Search City name
        private string _searchCity;
        public string SearchCity
        {
            get => _searchCity;
            set
            {
                if (_searchCity != value)
                {
                    _searchCity = value;
                    OnPropertyChanged(nameof(SearchCity));
                    UpdateFilteredCameras(); // Update filtered data in real-time
                }
            }
        }

        // Status message
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // Update filtered camera data
        private void UpdateFilteredCameras()
        {
            if (string.IsNullOrWhiteSpace(SearchCity))
            {
                // If the search box is empty, show all data
                FilteredCameras = new ObservableCollection<CameraInfo>(Cameras);
            }
            else
            {
                // Filter data based on City name
                var filtered = Cameras.Where(c => c.City.Contains(SearchCity, StringComparison.OrdinalIgnoreCase)).ToList();
                FilteredCameras = new ObservableCollection<CameraInfo>(filtered);
            }
        }

        // Whether to show the list
        private bool _isListViewVisible = true;
        public bool IsListViewVisible
        {
            get => _isListViewVisible;
            set
            {
                if (_isListViewVisible != value)
                {
                    _isListViewVisible = value;
                    OnPropertyChanged(nameof(IsListViewVisible));
                }
            }
        }

        // Whether to show the map
        private bool _isMapViewVisible = false;
        public bool IsMapViewVisible
        {
            get => _isMapViewVisible;
            set
            {
                if (_isMapViewVisible != value)
                {
                    _isMapViewVisible = value;
                    OnPropertyChanged(nameof(IsMapViewVisible));
                }
            }
        }

        // Icon for the map/list toggle button
        private string _mapButtonIcon = "🗺️";
        public string MapButtonIcon
        {
            get => _mapButtonIcon;
            set
            {
                if (_mapButtonIcon != value)
                {
                    _mapButtonIcon = value;
                    OnPropertyChanged(nameof(MapButtonIcon));
                }
            }
        }

        // Toggle between map and list view
        private void ToggleMapView()
        {
            IsListViewVisible = !IsListViewVisible;
            IsMapViewVisible = !IsMapViewVisible;

            // Update button icon
            MapButtonIcon = IsMapViewVisible ? "📋" : "🗺️";

            // If switching to map mode, trigger map update
            if (IsMapViewVisible)
            {
                UpdateMapRequested?.Invoke(this, Cameras);
            }
        }

        // IsEnabled state of the button
        public bool IsConnectButtonEnabled => SelectedCamera != null;

        // Selected camera
        private CameraInfo _selectedCamera;
        public CameraInfo SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                if (_selectedCamera != value)
                {
                    _selectedCamera = value;
                    OnPropertyChanged(nameof(SelectedCamera));
                    OnCameraSelected(value); // Trigger selected event

                    // Update the IsEnabled status of the button
                    OnPropertyChanged(nameof(IsConnectButtonEnabled));
                }
            }
        }

        // 定义事件
        public event EventHandler<bool> ConnectionStateChanged;

        // Connection status
        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                    OnPropertyChanged(nameof(ConnectionButtonText)); // Update button text

                    // Triggers an event to notify connection status changes
                    ConnectionStateChanged?.Invoke(this, value);
                }
            }
        }

        // Connection button text
        public string ConnectionButtonText => IsConnected ? "Disconnect" : "Connect";

        // Handle connection button click
        private void OnConnectionButtonClicked()
        {
            if (IsConnected)
            {
                // Disconnect
                DisconnectCamera();
            }
            else
            {
                // Connect
                ConnectCamera();
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



        // Connect to camera
        private void ConnectCamera()
        {
            if (SelectedCamera != null)
            {
                IsConnected = true;
                UpdateWebViewSource(SelectedCamera.IpAddress, SelectedCamera.Port);
            }
        }

        // Disconnect from camera
        private void DisconnectCamera()
        {
            IsConnected = false;
            WebViewSource = null;
        }

        private void UpdateWebViewSource(string ip, string port)
        {
            if (!string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port))
            {
                WebViewSource = $"http://{ip}:{port}/stream";
            }
            else
            {
                WebViewSource = null;
            }
            WebViewSourceUpdated?.Invoke(this, WebViewSource);
            OnPropertyChanged(nameof(WebViewSource));
        }

        // Event to notify WebView source update
        public event EventHandler<string> WebViewSourceUpdated;

        private void OnCameraSelected(CameraInfo selectedCamera)
        {
            if (selectedCamera != null)
            {
                // Update status message
                StatusMessage = $"Selected Camera: {selectedCamera.City}, {selectedCamera.Street}";

                // Trigger map update event
                UpdateMapRequested?.Invoke(this, new List<CameraInfo> { selectedCamera });
            }
        }

        // Define an event to notify the View to update the map
        public event EventHandler<IEnumerable<CameraInfo>> UpdateMapRequested;

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}