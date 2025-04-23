using Camera.Model;
using Camera.Datebase;
using MySqlConnector;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Camera.ViewModel
{
    public class CameraDashboardViewModel : INotifyPropertyChanged
    {
        private readonly DatebaseService _databaseService;
        private WebView _webView; // Reference to WebView, for calling JavaScript from ViewModel

        // WebView stream URL
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

        // Currently selected camera (e.g., from table)
        private CameraInfo _selectedCamera;
        public CameraInfo SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                if (_selectedCamera != value)
                {
                    _selectedCamera = value;
                    OnPropertyChanged();

                    // When user selects a camera from the table, center the map on it
                    if (_selectedCamera != null &&
                        double.TryParse(_selectedCamera.PositionLat, out double lat) &&
                        double.TryParse(_selectedCamera.PositionLong, out double lon) &&
                        _webView != null)
                    {
                        _webView.Eval($"focusCamera({lat}, {lon});");
                    }
                }
            }
        }

        public CameraDashboardViewModel()
        {
            _databaseService = new DatebaseService();

            Cameras = new ObservableCollection<CameraInfo>();
            FilteredCameras = new ObservableCollection<CameraInfo>();
            LoadCamerasFromDatabase();
        }

        // Bind WebView to ViewModel so JavaScript can be called directly
        public void SetWebView(WebView webView)
        {
            _webView = webView;
            LoadMapHtml();

            _webView.Navigating += async (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("✅ WebView loaded: " + e.Url);
                // Intercept custom scheme calls like: cameracommand://stream?id=1
                if (e.Url.StartsWith("cameracommand://"))
                {
                    e.Cancel = true;

                    var uri = new Uri(e.Url);
                    var cmd = uri.Host;
                    var idStr = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("id");

                    if (!int.TryParse(idStr, out int cameraId))
                        return;

                    var cam = Cameras.FirstOrDefault(c => c.Id == cameraId);
                    if (cam == null)
                        return;

                    if (cmd == "stream")
                    {
                        SelectedCamera = cam;
                        UpdateWebViewSource(cam.IpAddress, cam.Port);
                        await Shell.Current.GoToAsync("StreamPage");
                    }
                    else if (cmd == "live")
                    {
                        await Shell.Current.GoToAsync("LivePage");
                    }
                    if (cmd == "analyse")
                    {
                        // TODO: add analyse logic
                    }
                }
            };
        }

        // Load map.html content and inject into WebView
        private async void LoadMapHtml()
        {
            var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();
            _webView.Source = new HtmlWebViewSource {Html = html};
            await Task.Delay(300); // Wait for WebView to be fully ready
            UpdateFilteredAndMap();
        }

        // Full list of cameras from DB
        private ObservableCollection<CameraInfo> _cameras;
        public ObservableCollection<CameraInfo> Cameras
        {
            get => _cameras;
            set
            {
                if (_cameras != value)
                {
                    _cameras = value;
                    OnPropertyChanged();
                    UpdateFilteredAndMap();
                }
            }
        }

        // Filtered camera list (based on search)
        private ObservableCollection<CameraInfo> _filteredCameras;
        public ObservableCollection<CameraInfo> FilteredCameras
        {
            get => _filteredCameras;
            set
            {
                if (_filteredCameras != value)
                {
                    _filteredCameras = value;
                    OnPropertyChanged();
                }
            }
        }

        // Text typed into the search box
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    UpdateFilteredAndMap();
                }
            }
        }

        // Update table and map based on filter
        private void UpdateFilteredAndMap()
        {
            if (Cameras == null || Cameras.Count == 0 || _webView == null) return;

            // Filter camera list
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Cameras
                : new ObservableCollection<CameraInfo>(
                    Cameras.Where(c => c.City.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                       c.Street.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

            FilteredCameras = filtered;

            if (FilteredCameras.Count == 0)
                return;

            // Focus map on the first filtered camera
            var first = FilteredCameras.First();
            if (double.TryParse(first.PositionLat, out double centerLat) &&
                double.TryParse(first.PositionLong, out double centerLon))
            {
                _webView.Eval($"focusCamera({centerLat}, {centerLon});");
            }

            // Re-render markers on map
            foreach (var cam in FilteredCameras)
            {
                if (double.TryParse(cam.PositionLat, out double lat) &&
                    double.TryParse(cam.PositionLong, out double lon))
                {
                    string js = $"addCameraMarker('{cam.Id}', '{cam.City}', '{cam.Street}', '{cam.IpAddress}', '{cam.Port}', {lat}, {lon});";
                    _webView.Eval(js);
                }
            }
        }

        // Read camera data from MySQL
        public void LoadCamerasFromDatabase()
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();
                var query = "SELECT * FROM cameras";
                using var command = new MySqlCommand(query, connection);
                using var reader = command.ExecuteReader();

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
            catch (Exception ex)
            {
                // Log error, but don't crash UI
                System.Diagnostics.Debug.WriteLine($"[DB ERROR] {ex.Message}");
                Cameras.Clear();
                FilteredCameras.Clear();
            }
        }

        // Update WebView source for streaming
        private void UpdateWebViewSource(string ip, string port)
        {
            if (!string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port))
            {
                //WebViewSource = $"http://{ip}:{port}/stream";
                WebViewSource = $"http://{ip}:{port}/stream.html?src=scout_mini_1&mode=webrtc,mse,hls,mjpeg";
            }
            else
            {
                WebViewSource = null;
            }

            WebViewSourceUpdated?.Invoke(this, WebViewSource);
            OnPropertyChanged(nameof(WebViewSource));
        }

        // Notify other components when WebView source changes
        public event EventHandler<string> WebViewSourceUpdated;

        // Property change notification
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
