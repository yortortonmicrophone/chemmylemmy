using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Net.Http;
using System.Collections.Generic; // Added for List<string>

namespace chemmylemmy
{
    public partial class SearchBarWindow : Window
    {
        private double? lastMolarMass = null;
        private bool isPubChemResult = false;
        private static string previousText = "";
        private bool isClosing = false;
        private Settings settings;
        private DispatcherTimer debounceTimer;
        private const int DebounceMilliseconds = 500;

        public SearchBarWindow(Settings settings)
        {
            this.settings = settings;
            InitializeComponent();

            debounceTimer = new DispatcherTimer();
            debounceTimer.Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds);
            debounceTimer.Tick += DebounceTimer_Tick;
            
            // Set up PubChem logging
            PubChemService.LogCallback = (message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ResultsTextBlock.Text = $"🔍 {message}";
                });
            };
            
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

        private async void SearchBarWindow_Loaded(object sender, RoutedEventArgs e)
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
            
            // Test simple connectivity first
            ResultsTextBlock.Text = "Testing network connectivity...";
            Console.WriteLine("=== Starting connectivity tests ===");
            
            var simpleConnectivity = await PubChemService.TestSimpleConnectivityAsync();
            if (!simpleConnectivity)
            {
                ResultsTextBlock.Text = "❌ Network connectivity test failed. Check your internet connection.";
                return;
            }
            
            ResultsTextBlock.Text = "✅ Network connectivity OK. Testing PubChem...";
            
            // Test PubChem connectivity
            var isConnected = await PubChemService.TestConnectivityAsync();
            if (!isConnected)
            {
                ResultsTextBlock.Text = "⚠️ Warning: Cannot reach PubChem. Check your firewall or try again later.";
                return;
            }
            
            ResultsTextBlock.Text = "✅ PubChem connectivity OK. Ready to search!";
            
            // Test with glucose to verify everything works
            Console.WriteLine("Testing glucose search...");
            var glucoseTest = await PubChemService.TestWithGlucoseAsync();
            if (glucoseTest.Success)
            {
                Console.WriteLine($"Glucose test successful: MW = {glucoseTest.Compound.MolecularWeight}");
            }
            else
            {
                Console.WriteLine($"Glucose test failed: {glucoseTest.Error}");
            }
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
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        private async void DebounceTimer_Tick(object? sender, EventArgs e)
        {
            debounceTimer.Stop();
            await UpdateResults();
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
                        ShowDebugConfirmation(copiedValue, isPubChemResult);
                    }
                    
                    // Store current text before closing
                    previousText = SearchTextBox.Text;
                    Close();
                }
            }
        }

        private void ShowDebugConfirmation(string copiedValue, bool isPubChem = false)
        {
            string title = isPubChem ? "PubChem - Copied!" : "Debug - Copied!";
            string text = isPubChem ? $"Copied: {copiedValue} g/mol (PubChem)" : $"Copied: {copiedValue} g/mol";
            
            var debugWindow = new Window
            {
                Title = title,
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
                        Text = text,
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

        private async Task UpdateResults()
        {
            string query = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                ResultsTextBlock.Text = "Results will appear here...";
                lastMolarMass = null;
                isPubChemResult = false;
                return;
            }

            // Check for explicit method prefixes
            if (query.StartsWith("mm ", StringComparison.OrdinalIgnoreCase))
            {
                // Force local molar mass parsing only
                var searchTerm = query.Substring(3).Trim();
                if (string.IsNullOrEmpty(searchTerm))
                {
                    ResultsTextBlock.Text = "Enter a formula after 'mm ' for local parsing";
                    lastMolarMass = null;
                    isPubChemResult = false;
                    return;
                }
                
                var mmResult = SmartFormulaParser.ParseFormula(searchTerm);
                if (mmResult.Success)
                {
                    lastMolarMass = mmResult.MolarMass;
                    isPubChemResult = false;
                    
                    var sb = new StringBuilder();
                    sb.AppendLine($"Local Formula Parsing: {searchTerm}");
                    sb.AppendLine($"Method: {mmResult.ParsedFormula}");
                    
                    string format = $"F{settings.DecimalPlaces}";
                    sb.AppendLine($"Molar Mass: {mmResult.MolarMass.ToString(format)} g/mol");
                    
                    foreach (var line in mmResult.Breakdown)
                        sb.AppendLine(line);
                    
                    ResultsTextBlock.Text = sb.ToString().TrimEnd('\n', '\r');
                }
                else
                {
                    lastMolarMass = null;
                    isPubChemResult = false;
                    ResultsTextBlock.Text = $"Local parsing failed: {mmResult.Error}";
                }
                return;
            }
            
            if (query.StartsWith("pu ", StringComparison.OrdinalIgnoreCase))
            {
                // Force PubChem search only
                var searchTerm = query.Substring(3).Trim();
                if (string.IsNullOrEmpty(searchTerm))
                {
                    ResultsTextBlock.Text = "Enter a compound name after 'pu ' for PubChem search";
                    lastMolarMass = null;
                    isPubChemResult = false;
                    return;
                }
                
                ResultsTextBlock.Text = $"🔍 Searching PubChem for '{searchTerm}'...";
                var pubChemResult = await PubChemService.SearchCompoundAsync(searchTerm);
                
                if (pubChemResult.Success)
                {
                    await HandlePubChemResult(pubChemResult);
                }
                else
                {
                    lastMolarMass = null;
                    isPubChemResult = false;
                    ResultsTextBlock.Text = $"PubChem search failed: {pubChemResult.Error}";
                }
                return;
            }

            // Default behavior: try local parsing first, then PubChem
            var localResult = SmartFormulaParser.ParseFormula(query);
            
            if (localResult.Success)
            {
                // Local parsing worked - show result immediately
                lastMolarMass = localResult.MolarMass;
                isPubChemResult = false;
                
                var sb = new StringBuilder();
                sb.AppendLine($"Local Formula Parsing: {query}");
                sb.AppendLine($"Method: {localResult.ParsedFormula}");
                
                // Format molar mass with proper decimal places
                string format = $"F{settings.DecimalPlaces}";
                sb.AppendLine($"Molar Mass: {localResult.MolarMass.ToString(format)} g/mol");
                
                foreach (var line in localResult.Breakdown)
                    sb.AppendLine(line);
                
                ResultsTextBlock.Text = sb.ToString().TrimEnd('\n', '\r');
            }
            else
            {
                // Local parsing failed, try PubChem
                ResultsTextBlock.Text = $"🔍 Local parsing failed, searching PubChem for '{query}'...";
                var pubChemResult = await PubChemService.SearchCompoundAsync(query);
                
                if (pubChemResult.Success)
                {
                    // PubChem found the compound
                    await HandlePubChemResult(pubChemResult);
                }
                else
                {
                    // Neither local parsing nor PubChem worked
                    lastMolarMass = null;
                    isPubChemResult = false;
                    ResultsTextBlock.Text = $"Not found in local database or PubChem.\nLocal error: {localResult.Error}\nPubChem error: {pubChemResult.Error}";
                }
            }
        }

        private async Task HandlePubChemResult(PubChemSearchResult pubChemResult)
        {
            var compound = pubChemResult.Compound;
            lastMolarMass = compound.MolecularWeight;
            isPubChemResult = true;
            
            var sb = new StringBuilder();
            sb.AppendLine($"PubChem Search: {pubChemResult.SearchTerm}");
            sb.AppendLine($"CID: {compound.CID}");
            
            if (!string.IsNullOrEmpty(compound.MolecularFormula))
                sb.AppendLine($"Formula: {compound.MolecularFormula}");
            
            // Format molecular weight with proper decimal places
            string format = $"F{settings.DecimalPlaces}";
            sb.AppendLine($"Molecular Weight: {compound.MolecularWeight.ToString(format)} g/mol");
            
            ResultsTextBlock.Text = sb.ToString().TrimEnd('\n', '\r');
        }
    }
} 