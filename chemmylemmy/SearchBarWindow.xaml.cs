using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Windows.Threading;

namespace chemmylemmy
{
    public partial class SearchBarWindow : Window
    {
        private double? lastMolarMass = null;
        private static string previousText = "";
        private bool isClosing = false;

        public SearchBarWindow()
        {
            InitializeComponent();
            
            // Restore previous text if available
            if (!string.IsNullOrEmpty(previousText))
            {
                SearchTextBox.Text = previousText;
                SearchTextBox.SelectAll(); // Select all text when reopening
            }
            
            // Subscribe to window deactivation to close when other apps gain focus
            this.Deactivated += SearchBarWindow_Deactivated;
            
            // Subscribe to window loaded event to ensure focus
            this.Loaded += SearchBarWindow_Loaded;
            
            // Subscribe to window activated event
            this.Activated += SearchBarWindow_Activated;
        }

        private void SearchBarWindow_Activated(object sender, EventArgs e)
        {
            // Ensure focus when window is activated
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                SearchTextBox.Focus();
                if (!string.IsNullOrEmpty(SearchTextBox.Text))
                {
                    SearchTextBox.SelectAll();
                }
            }));
        }

        private void SearchBarWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Use Dispatcher to ensure focus is set after the window is fully loaded
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                SearchTextBox.Focus();
                if (!string.IsNullOrEmpty(SearchTextBox.Text))
                {
                    SearchTextBox.SelectAll();
                }
            }));
        }

        private void SearchBarWindow_Deactivated(object? sender, EventArgs e)
        {
            if (!isClosing)
            {
                isClosing = true;
                // Store current text before closing
                previousText = SearchTextBox.Text;
                Close();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateResults();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (!isClosing)
                {
                    isClosing = true;
                    // Store current text before closing
                    previousText = SearchTextBox.Text;
                    Close();
                }
            }
            else if (e.Key == Key.Enter && lastMolarMass.HasValue)
            {
                if (!isClosing)
                {
                    isClosing = true;
                    // Copy to clipboard
                    Clipboard.SetText(lastMolarMass.Value.ToString("F3"));
                    // Store current text before closing
                    previousText = SearchTextBox.Text;
                    Close();
                }
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