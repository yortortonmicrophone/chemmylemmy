using System.Windows;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace chemmylemmy
{
    public partial class SearchBarWindow : Window
    {
        private double? lastMolarMass = null;
        public SearchBarWindow()
        {
            InitializeComponent();
            // Clip the window to a rounded rectangle for true rounded corners
            void UpdateClip() => this.Clip = new System.Windows.Media.RectangleGeometry(new System.Windows.Rect(0, 0, this.ActualWidth, this.ActualHeight), 7, 7);
            this.Loaded += (s, e) =>
            {
                UpdateClip();
            };
            this.SizeChanged += (s, e) => UpdateClip();
            SearchTextBox.Focus();
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;
            SearchTextBox.KeyDown += SearchTextBox_KeyDown;
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && lastMolarMass.HasValue)
            {
                System.Windows.Clipboard.SetText(lastMolarMass.Value.ToString());
                ResultsTextBlock.Text += "\n(Molar mass copied to clipboard)";
                e.Handled = true;
            }
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                ResultsTextBlock.Text = "Results will appear here...";
                lastMolarMass = null;
                return;
            }

            var result = FormulaParser.ParseAndCalculateMolarMass(query);
            if (result.Success)
            {
                lastMolarMass = result.MolarMass;
                var sb = new StringBuilder();
                sb.AppendLine($"Molar Mass: {result.MolarMass} g/mol");
                foreach (var line in result.Breakdown)
                    sb.AppendLine(line);
                ResultsTextBlock.Text = sb.ToString().TrimEnd('\n', '\r');
            }
            else
            {
                lastMolarMass = null;
                ResultsTextBlock.Text = $"Error: {result.Error}";
            }
        }
    }
} 