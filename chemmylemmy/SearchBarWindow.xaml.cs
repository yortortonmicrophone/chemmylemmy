using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace chemmylemmy
{
    public partial class SearchBarWindow : Window
    {
        private double? lastMolarMass = null;
        private static string previousText = "";
        private bool isClosing = false;
        private Settings settings;

        public SearchBarWindow(Settings settings)
        {
            this.settings = settings;
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
            if (!isClosing && settings.AutoHideOnFocusLoss)
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
                    // Copy to clipboard with proper decimal places
                    string format = $"F{settings.DecimalPlaces}";
                    string copiedValue = lastMolarMass.Value.ToString(format);
                    Clipboard.SetText(copiedValue);
                    
                    // Show debug confirmation window if enabled
                    if (settings.ShowCopyConfirmation)
                    {
                        ShowDebugConfirmation(copiedValue);
                    }
                    
                    // Store current text before closing
                    previousText = SearchTextBox.Text;
                    Close();
                }
            }
        }

        private void ShowDebugConfirmation(string copiedValue)
        {
            var debugWindow = new Window
            {
                Title = "Debug - Copied!",
                Width = 200,
                Height = 60,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = System.Windows.Media.Brushes.Transparent,
                AllowsTransparency = true,
                Content = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(15),
                    Background = System.Windows.Media.Brushes.DarkGray,
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = $"Copied: {copiedValue} g/mol",
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    }
                }
            };
            
            debugWindow.Show();
            
            // Create fade-out animation
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(500) // Start fade-out after 0.5 seconds
            };
            
            // Create storyboard
            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeOut);
            
            // Set target
            Storyboard.SetTarget(fadeOut, debugWindow);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            
            // Handle completion
            storyboard.Completed += (s, e) =>
            {
                debugWindow.Close();
            };
            
            // Start animation
            storyboard.Begin();
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
                
                // Format molar mass with proper decimal places
                string format = $"F{settings.DecimalPlaces}";
                sb.AppendLine($"Molar Mass: {result.MolarMass.ToString(format)} g/mol");
                
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