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
        private WebView _webView;

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
                    OnPropertyChanged();

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
            var connectionString = "server=192.168.31.151;port=3306;database=traffic_analysis;user=root;password=123456;";
            _databaseService = new DatebaseService(connectionString);

            Cameras = new ObservableCollection<CameraInfo>();
            FilteredCameras = new ObservableCollection<CameraInfo>();
            LoadCamerasFromDatabase();
        }

        public void SetWebView(WebView webView)
        {
            _webView = webView;
            LoadMapHtml();

            _webView.Navigating += async (s, e) =>
            {
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
                        await Shell.Current.GoToAsync("//MainPage");
                    }
                    else if (cmd == "live")
                    {
                        await Shell.Current.GoToAsync("//LivePage");
                    }
                    if (cmd == "analyse")
                    {
                        
                    }
                }
            };
        }

        private async void LoadMapHtml()
        {
            var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();
            _webView.Source = new HtmlWebViewSource { Html = html };

            await Task.Delay(300);
            UpdateFilteredAndMap();
        }

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

        private void UpdateFilteredAndMap()
        {
            if (Cameras == null || Cameras.Count == 0 || _webView == null) return;

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Cameras
                : new ObservableCollection<CameraInfo>(
                    Cameras.Where(c => c.City.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                       c.Street.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

            FilteredCameras = filtered;

            if (FilteredCameras.Count == 0)
                return;

            // 自动居中到第一个符合条件的摄像头
            var first = FilteredCameras.First();
            if (double.TryParse(first.PositionLat, out double centerLat) &&
                double.TryParse(first.PositionLong, out double centerLon))
            {
                _webView.Eval($"focusCamera({centerLat}, {centerLon});");
            }


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
                // ✅ 日志可选：输出错误到 Debug 或日志系统
                System.Diagnostics.Debug.WriteLine($"[DB ERROR] {ex.Message}");

                // ✅ 不抛出异常，不阻止 UI 加载
                Cameras.Clear();
                FilteredCameras.Clear();
            }
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


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
