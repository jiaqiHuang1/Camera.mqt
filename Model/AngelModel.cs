namespace Camera.Model
{
    public class ArcSliderModel
    {
        // 1. Stores the angle of the slider
        public float Angle { get; set; } = 0;

        // 2. Indicates if the slider is in editing mode
        public bool IsEditing { get; set; } = false;

        // 3. Stores the text content of the input box
        public string InputText { get; set; } = string.Empty;

        // 4. Controls the visibility of the input box
        public bool IsEntryVisible { get; set; } = false;
    }
}
