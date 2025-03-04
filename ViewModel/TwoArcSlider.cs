using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Camera.ViewModel
{
    public class TwoArcSlider : INotifyPropertyChanged
    {
        private readonly CamerasViewModel _camerasViewModel;
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

        // Constructor: Initialize two ViewModel instances
        public TwoArcSlider()
        {
            SliderViewModel = new ArcSliderViewModel();
            Slider_VerViewModel = new ArcSlider_VerViewModel();

            // **Get from Service Locator CamerasViewModel**
            _camerasViewModel = MauiProgram.Services.GetService<CamerasViewModel>();


            if (_camerasViewModel != null)
            {
                // **Listening for CamerasViewModel.WebViewSource changes**
                _camerasViewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(CamerasViewModel.WebViewSource))
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
        }


        // **update WebViewSource**
        private void UpdateWebViewSource()
        {
            WebViewSource = _camerasViewModel?.WebViewSource;
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
