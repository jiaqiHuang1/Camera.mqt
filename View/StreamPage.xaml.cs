using Camera.ViewModel;

namespace Camera.View
{
    public partial class StreamPage : ContentPage
    {
        private TwoArcSlider _viewModel;

        public StreamPage(TwoArcSlider viewModel)
        {
            InitializeComponent();
            //_viewModel = BindingContext as TwoArcSlider;
            _viewModel = viewModel;
            BindingContext = _viewModel;
            // Subscribe to the RequestRedraw event, and call Invalidate() to redraw when the event is triggered
            _viewModel.SliderViewModel.RequestRedraw += () => arcSlider.Invalidate();
            _viewModel.Slider_VerViewModel.RequestRedraw += () => arcSlider_Ver.Invalidate();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is TwoArcSlider viewModel)
            {
                await viewModel.OnPageAppearingAsync();
            }
        }

        // During sliding interaction
        private void ArcSlider_DragInteraction(object sender, TouchEventArgs e)
        {
            UpdateAngle(e, _viewModel.SliderViewModel);
        }

        private void ArcSlider_EndInteraction(object sender, TouchEventArgs e)
        {
            UpdateAngle(e, _viewModel.SliderViewModel);
            _viewModel.SliderViewModel.EndHandleSelection();
        }

        private void ArcSlider_Ver_DragInteraction(object sender, TouchEventArgs e)
        {
            UpdateAngle_Ver(e, _viewModel.Slider_VerViewModel);
        }

        private void ArcSlider_Ver_EndInteraction(object sender, TouchEventArgs e)
        {
            UpdateAngle_Ver(e, _viewModel.Slider_VerViewModel);
            _viewModel.Slider_VerViewModel.EndHandleSelection();
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
                else
                {
                    _viewModel.SliderViewModel.StartHandleSelection(touchX, touchY,
                _viewModel.SliderViewModel.ArcSliderDrawable._centerX,
                _viewModel.SliderViewModel.ArcSliderDrawable._centerY);
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
                else
                {
                    _viewModel.Slider_VerViewModel.StartHandleSelection(touchX, touchY,
                _viewModel.Slider_VerViewModel.ArcSlider_VerDrawable._centerX,
                _viewModel.Slider_VerViewModel.ArcSlider_VerDrawable._centerY);
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