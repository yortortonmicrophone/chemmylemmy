using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace chemmylemmy
{
    public partial class SearchBarWindow : Window
    {
        private double? lastMolarMass = null;

        public SearchBarWindow()
        {
            InitializeComponent();
            SearchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateResults();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Enter && lastMolarMass.HasValue)
            {
                // Copy to clipboard
                Clipboard.SetText(lastMolarMass.Value.ToString("F3"));
                Close();
            }
        }

        private void UpdateResults()
        {
            string query = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                ResultsTextBlock.Text = "Results will appear here...";
                lastMolarMass = null;
                return;
            }

            var result = SmartFormulaParser.ParseFormula(query);
            if (result.Success)
            {
                lastMolarMass = result.MolarMass;
                var sb = new StringBuilder();
                sb.AppendLine($"Method: {result.ParsedFormula}");
                sb.AppendLine($"Molar Mass: {result.MolarMass:F3} g/mol");
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