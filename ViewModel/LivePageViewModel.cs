using System.Collections.ObjectModel;
using System.ComponentModel;
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
            Vehicles.CollectionChanged += (s, e) => InvalidateGraphicsView(); // Listens for data changes and automatically refreshes the `GraphicsView`.

            VehicleCanvas = new VehicleDrawable(Vehicles); //  The `Drawable` is only responsible for drawing.

            LoadVehicles();

            // Timer automatically updates every 0.5 seconds
            _autoUpdateTimer = Application.Current.Dispatcher.CreateTimer();
            _autoUpdateTimer.Interval = TimeSpan.FromMilliseconds(500);
            _autoUpdateTimer.Tick += (s, e) =>
            {
                Console.WriteLine("🚀 timer-triggered LoadVehicles()");
                LoadVehicles();
            };
            _autoUpdateTimer.Start();
        }

        public void LoadVehicles()
        {
            Console.WriteLine($"LoadVehicles() invoked: {DateTime.Now}");
            Vehicles.Clear();
            StatusMessage = "Loading vehicle information...";
            OnPropertyChanged(nameof(StatusMessage));

            try
            {
                using (var connection = _databaseService.GetConnection())
                {
                    connection.Open();
                    var query = "SELECT * FROM vehicles";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Vehicles.Add(new VehiclesInfo
                            {
                                Id = reader.GetInt32("id"),
                                X = reader.GetDouble("position_x"),
                                Y = reader.GetDouble("position_y"),
                                Width = reader.GetDouble("width"),
                                Length = reader.GetDouble("height"),
                                Type = reader.GetInt32("type")
                            });
                        }
                    }
                }

                StatusMessage = "Vehicle information loaded successfully";
                OnPropertyChanged(nameof(Vehicles));
                OnPropertyChanged(nameof(StatusMessage));

                InvalidateGraphicsView();
            }
            catch (Exception ex)
            {
                StatusMessage = $"failed to load: {ex.Message}";
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        private void InvalidateGraphicsView()
        {
            Application.Current.Dispatcher.Dispatch(() =>
            {
                if (Application.Current?.MainPage is not null)
                {
                    var graphicsView = Application.Current.MainPage.FindByName<GraphicsView>("VehicleGraphicsView");
                    if (graphicsView != null)
                    {
                        Console.WriteLine(" trig GraphicsView Invalidate()");
                        graphicsView.Invalidate();  
                    }
                    else
                    {
                        Console.WriteLine("⚠️ not found GraphicsView");
                    }
                }
            });
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
