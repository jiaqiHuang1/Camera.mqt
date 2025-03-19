using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Maui.Dispatching;
using MySqlConnector;
using Camera.Model;
using Camera.Datebase;
using Camera.Drawables;

namespace Camera.ViewModel
{
    public class LivePageViewModel : INotifyPropertyChanged
    {
        private readonly IDispatcherTimer _autoUpdateTimer;
        private readonly DatebaseService _databaseService;

        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<VehiclesInfo> Vehicles { get; set; }
        public string StatusMessage { get; set; }
        public IDrawable VehicleCanvas { get; private set; }

        public LivePageViewModel()
        {
            var connectionString = "server=192.168.31.151;port=3306;database=traffic_analysis;user=root;password=123456;";
            _databaseService = new DatebaseService(connectionString);

            Vehicles = new ObservableCollection<VehiclesInfo>();
            VehicleCanvas = new VehicleDrawable(Vehicles); // Responsible for rendering only

            // Listen for data changes because the UI needs to refresh when the data updates
            Vehicles.CollectionChanged += (s, e) => InvalidateGraphicsView();

            LoadVehicles();

            // Reloads vehicle data every 1 second because periodic updates are required
            _autoUpdateTimer = Application.Current.Dispatcher.CreateTimer();
            //_autoUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            _autoUpdateTimer.Interval = TimeSpan.FromMilliseconds(500);
            _autoUpdateTimer.Tick += (s, e) => LoadVehicles();
            _autoUpdateTimer.Start();
        }

        public void LoadVehicles()
        {
            Application.Current.Dispatcher.Dispatch(() =>
            {
                Vehicles.Clear();
                StatusMessage = "Loading vehicle information...";
                OnPropertyChanged(nameof(StatusMessage));
            });

            try
            {
                using (var connection = _databaseService.GetConnection())
                {
                    connection.Open();
                    var query = "SELECT * FROM vehicles";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        List<VehiclesInfo> tempVehicles = new();

                        while (reader.Read())
                        {
                            tempVehicles.Add(new VehiclesInfo
                            {
                                Id = reader.GetInt32("id"),
                                X = reader.GetDouble("position_x"),
                                Y = reader.GetDouble("position_y"),
                                Width = reader.GetDouble("width"),
                                Length = reader.GetDouble("height"),
                                Type = reader.GetInt32("type")
                            });
                        }

                        // Update UI thread because `Vehicles` is bound to the UI
                        Application.Current.Dispatcher.Dispatch(() =>
                        {
                            foreach (var vehicle in tempVehicles)
                            {
                                Vehicles.Add(vehicle);
                            }

                            StatusMessage = "Vehicle information loaded successfully";
                            OnPropertyChanged(nameof(StatusMessage));
                            InvalidateGraphicsView();
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() =>
                {
                    StatusMessage = $"Failed to load: {ex.Message}";
                    OnPropertyChanged(nameof(StatusMessage));
                });
            }
        }

        private void InvalidateGraphicsView()
        {
            Application.Current.Dispatcher.Dispatch(() =>
            {
                var currentPage = Application.Current.MainPage?.Navigation?.NavigationStack.LastOrDefault();
                var graphicsView = currentPage?.FindByName<GraphicsView>("VehicleGraphicsView");

                if (graphicsView != null)
                {
                    graphicsView.Invalidate(); // Triggers a redraw because `GraphicsView` needs to update its content
                }
            });
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
