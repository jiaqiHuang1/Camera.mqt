using Camera.ViewModel;

namespace Camera.View
{
    public partial class MainPage : ContentPage
    {
        private TwoArcSlider _viewModel;

        public MainPage(TwoArcSlider viewModel)
        {
            InitializeComponent();
            //_viewModel = BindingContext as TwoArcSlider;
            _viewModel = viewModel; 
            BindingContext = _viewModel; 
            // Subscribe to the RequestRedraw event, and call Invalidate() to redraw when the event is triggered
            _viewModel.SliderViewModel.RequestRedraw += () => arcSlider.Invalidate();
            _viewModel.Slider_VerViewModel.RequestRedraw += () => arcSlider_Ver.Invalidate();
        }

        // During sliding interaction
        private void ArcSlider_DragInteraction(object sender, TouchEventArgs e)
        {
            UpdateAngle(e, _viewModel.SliderViewModel);
        }

        private void ArcSlider_EndInteraction(object sender, TouchEventArgs e)
        {
            UpdateAngle(e, _viewModel.SliderViewModel);
        }

        private void ArcSlider_Ver_DragInteraction(object sender, TouchEventArgs e)
        {
            UpdateAngle_Ver(e, _viewModel.Slider_VerViewModel);
        }

        private void ArcSlider_Ver_EndInteraction(object sender, TouchEventArgs e)
        {
            UpdateAngle_Ver(e, _viewModel.Slider_VerViewModel);
        }

        // Update angle
        private void UpdateAngle(TouchEventArgs e, ArcSliderViewModel sliderViewModel)
        {
            if (e.Touches.Count() > 0)
            {
                var touchX = (float)e.Touches[0].X;
                var touchY = (float)e.Touches[0].Y;

                sliderViewModel.UpdateAngle(touchX, touchY,
                                            sliderViewModel.ArcSliderDrawable._centerX,
                                            sliderViewModel.ArcSliderDrawable._centerY);
            }
        }

        private void UpdateAngle_Ver(TouchEventArgs e, ArcSlider_VerViewModel slider_VerViewModel)
        {
            if (e.Touches.Count() > 0)
            {
                var touchX = (float)e.Touches[0].X;
                var touchY = (float)e.Touches[0].Y;

                slider_VerViewModel.UpdateAngle(touchX, touchY,
                                            slider_VerViewModel.ArcSlider_VerDrawable._centerX,
                                            slider_VerViewModel.ArcSlider_VerDrawable._centerY);
            }
        }

        // Handle tap event to show input box
        private void ArcSlider_StartInteraction(object sender, TouchEventArgs e)
        {
            if (e.Touches.Count() > 0)
            {
                var touchX = (float)e.Touches[0].X;
                var touchY = (float)e.Touches[0].Y;

                // Check if the box is tapped
                bool isTappedOnBox = _viewModel.SliderViewModel.HandleTouch(touchX, touchY);

                System.Diagnostics.Debug.WriteLine($"[StartInteraction] isTappedOnBox: {isTappedOnBox}");

                if (isTappedOnBox)
                {
                    // Show input box
                    _viewModel.SliderViewModel.ShowEntry();
                }
            }
        }

        private void ArcSlider_Ver_StartInteraction(object sender, TouchEventArgs e)
        {
            if (e.Touches.Count() > 0)
            {
                var touchX = (float)e.Touches[0].X;
                var touchY = (float)e.Touches[0].Y;

                // Check if the box is tapped
                bool isTappedOnBox = _viewModel.Slider_VerViewModel.HandleTouch(touchX, touchY);

                System.Diagnostics.Debug.WriteLine($"[StartInteraction] isTappedOnBox: {isTappedOnBox}");

                if (isTappedOnBox)
                {
                    // Show input box
                    _viewModel.Slider_VerViewModel.ShowEntry();
                }
            }
        }

        // Update angle and exit input mode after input is completed
        private void AngleEntry_Completed(object sender, EventArgs e)
        {
            _viewModel.SliderViewModel.ConfirmInput();
        }

        private void AngleEntry_Ver_Completed(object sender, EventArgs e)
        {
            _viewModel.Slider_VerViewModel.ConfirmInput();
        }
    }
}