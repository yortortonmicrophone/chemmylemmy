namespace chemmylemmy
{
    public class ColorPreset
    {
        public string Name { get; set; } = "";
        public string SearchBoxBorderColor { get; set; } = "#FFFFFFFF";
        public string SearchBoxTextColor { get; set; } = "#FFF8F8F2";
        public string SearchBoxBackgroundColor { get; set; } = "#FF57584F";
        public string ResultsBoxBorderColor { get; set; } = "#FF49483E";
        public string ResultsBoxTextColor { get; set; } = "#FFF8F8F2";
        public string ResultsBoxBackgroundColor { get; set; } = "#FF35362F";
        public string WindowBorderColor { get; set; } = "#FF49483E";
        public string WindowBackgroundColor { get; set; } = "#FF272822";
        public string HighlightColor { get; set; } = "#FFA6E22E";
        public string NotificationBackgroundColor { get; set; } = "#FF57584F";
        public string NotificationBorderColor { get; set; } = "#FF49483E";
        public string NotificationTextColor { get; set; } = "#FFF8F8F2";

        public bool IsEmpty => string.IsNullOrEmpty(Name);

        public void CopyFrom(Settings settings)
        {
            SearchBoxBorderColor = settings.SearchBoxBorderColor;
            SearchBoxTextColor = settings.SearchBoxTextColor;
            SearchBoxBackgroundColor = settings.SearchBoxBackgroundColor;
            ResultsBoxBorderColor = settings.ResultsBoxBorderColor;
            ResultsBoxTextColor = settings.ResultsBoxTextColor;
            ResultsBoxBackgroundColor = settings.ResultsBoxBackgroundColor;
            WindowBorderColor = settings.WindowBorderColor;
            WindowBackgroundColor = settings.WindowBackgroundColor;
            HighlightColor = settings.HighlightColor;
            NotificationBackgroundColor = settings.NotificationBackgroundColor;
            NotificationBorderColor = settings.NotificationBorderColor;
            NotificationTextColor = settings.NotificationTextColor;
        }

        public void ApplyTo(Settings settings)
        {
            settings.SearchBoxBorderColor = SearchBoxBorderColor;
            settings.SearchBoxTextColor = SearchBoxTextColor;
            settings.SearchBoxBackgroundColor = SearchBoxBackgroundColor;
            settings.ResultsBoxBorderColor = ResultsBoxBorderColor;
            settings.ResultsBoxTextColor = ResultsBoxTextColor;
            settings.ResultsBoxBackgroundColor = ResultsBoxBackgroundColor;
            settings.WindowBorderColor = WindowBorderColor;
            settings.WindowBackgroundColor = WindowBackgroundColor;
            settings.HighlightColor = HighlightColor;
            settings.NotificationBackgroundColor = NotificationBackgroundColor;
            settings.NotificationBorderColor = NotificationBorderColor;
            settings.NotificationTextColor = NotificationTextColor;
        }
    }
} 