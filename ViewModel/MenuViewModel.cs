using System.ComponentModel;
using System.Windows.Input;


namespace Camera.ViewModel
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        private readonly CamerasViewModel _camerasViewModel;
        public ICommand CameraCommand { get; }
        public ICommand AnalysisCommand { get; }
        public ICommand LiveCommand { get; }
        public ICommand ControlCommand { get; }
        public ICommand SettingCommand { get; }

        private bool _isAnalysisEnabled;
        private bool _isLiveEnabled;
        private bool _isControlEnabled;

        public bool IsAnalysisEnabled
        {
            get => _isAnalysisEnabled;
            set
            {
                _isAnalysisEnabled = value;
                OnPropertyChanged(nameof(IsAnalysisEnabled));
                ((Command)AnalysisCommand).ChangeCanExecute(); // Trigger Button Status Refresh

            }
        }

        public bool IsLiveEnabled
        {
            get => _isLiveEnabled;
            set
            {
                _isLiveEnabled = value;
                OnPropertyChanged(nameof(IsLiveEnabled));
                ((Command)LiveCommand).ChangeCanExecute();
            }
        }

        public bool IsControlEnabled
        {
            get => _isControlEnabled;
            set
            {
                _isControlEnabled = value;
                OnPropertyChanged(nameof(IsControlEnabled));
                ((Command)ControlCommand).ChangeCanExecute();
            }
        }

        public MenuViewModel()
        {
            // Preventing MauiProgram.Services from being empty
            if (MauiProgram.Services == null)
            {
                throw new InvalidOperationException("MauiProgram.Services has not been initialised, check the MauiProgram.cs");
            }

            // Get CamerasViewModel via Service Locator
            _camerasViewModel = MauiProgram.Services.GetService<CamerasViewModel>();

            if (_camerasViewModel == null)
            {
                throw new InvalidOperationException("Cannot get CamerasViewModel, please check dependency injection configuration.");
            }

            // Subscribe to connection state change events
            _camerasViewModel.ConnectionStateChanged += OnConnectionStateChanged;

            CameraCommand = new Command(OnCameraClicked);
            AnalysisCommand = new Command(OnAnalysisClicked, () => IsAnalysisEnabled);
            LiveCommand = new Command(OnLiveClicked, () => IsLiveEnabled);
            ControlCommand = new Command(OnControlClicked, () => IsControlEnabled);
            SettingCommand = new Command(OnSettingClicked);


            // Initialise buttons according to current connection status
            UpdateButtonStates(_camerasViewModel.IsConnected);
        }

        private void OnConnectionStateChanged(object sender, bool isConnected)
        {

            UpdateButtonStates(isConnected);
        }

        private void UpdateButtonStates(bool isConnected)
        {
            IsAnalysisEnabled = isConnected;
            IsLiveEnabled = isConnected;
            IsControlEnabled = isConnected;
        }
        private async void OnCameraClicked()
        {
           
            await Shell.Current.GoToAsync("CameraDashboard");
           
        }

        private async void OnAnalysisClicked()
        {
            // Handle Analysis button click
        }

        private async void OnLiveClicked()
        {
            await Shell.Current.GoToAsync("LivePage");
        }

        private async void OnControlClicked()
        {
            await Shell.Current.GoToAsync("MainPage");
        }

        private async void OnSettingClicked()
        {
            await Shell.Current.GoToAsync("Setting_Wlan");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}