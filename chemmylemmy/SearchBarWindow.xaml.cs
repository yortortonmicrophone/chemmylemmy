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
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Net.Http;
using System.Collections.Generic; // Added for List<string>
using System.Windows.Media.Imaging;
using System.IO;

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
        private System.Windows.Media.ScaleTransform ScaleTransform;
        private int? lastPubChemCID = null; // Store last PubChem CID

        public SearchBarWindow(Settings settings)
        {
            this.settings = settings;
            InitializeComponent();

            // Apply color settings (robust)
            SearchBoxBorder.BorderBrush = SafeBrushFromString(settings.SearchBoxBorderColor, "#FFFFFFFF");
            SearchBoxBorder.Background = SafeBrushFromString(settings.SearchBoxBackgroundColor, "#FF57584F");
            SearchTextBox.Foreground = SafeBrushFromString(settings.SearchBoxTextColor, "#FFF8F8F2");
            SearchTextBox.Background = SafeBrushFromString(settings.SearchBoxBackgroundColor, "#FF57584F");
            ResultsBoxBorder.BorderBrush = SafeBrushFromString(settings.ResultsBoxBorderColor, "#FF49483E");
            ResultsBoxBorder.Background = SafeBrushFromString(settings.ResultsBoxBackgroundColor, "#FF35362F");
            ResultsTextBlock.Foreground = SafeBrushFromString(settings.ResultsBoxTextColor, "#FFF8F8F2");
            MainBorder.BorderBrush = SafeBrushFromString(settings.WindowBorderColor, "#FF49483E");
            MainBorder.Background = SafeBrushFromString(settings.WindowBackgroundColor, "#FF272822");
            // Highlight color for selection
            SearchTextBox.SelectionBrush = SafeBrushFromString(settings.HighlightColor, "#FFA6E22E");
            
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
            
            // Apply window scaling
            ApplyWindowScaling();
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
            
            // If there's previous text, show the results immediately
            if (!string.IsNullOrEmpty(SearchTextBox.Text))
            {
                await UpdateResults();
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
            // New: Ctrl+Enter opens PubChem page if result is from PubChem
            else if (e.Key == Key.Enter && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                if (isPubChemResult && lastPubChemCID.HasValue)
                {
                    string url = $"https://pubchem.ncbi.nlm.nih.gov/compound/{lastPubChemCID.Value}";
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        // Optionally show error to user
                        System.Windows.MessageBox.Show($"Failed to open PubChem page: {ex.Message}");
                    }
                }
            }
            // Only Enter (no Ctrl): copy molar mass
            else if (e.Key == Key.Enter && lastMolarMass.HasValue && !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                if (!isClosing)
                {
                    isClosing = true;
                    // Copy to clipboard with proper decimal places
                    string format = $"F{settings.DecimalPlaces}";
                    string copiedValue = lastMolarMass.Value.ToString(format);
                    Clipboard.SetText(copiedValue);
                    
                    // Store current text before closing
                    previousText = SearchTextBox.Text;
                    
                    // Show notification in a separate overlay window
                    ShowNotificationOverlay(copiedValue, isPubChemResult);
                    
                    // Close immediately
                    Close();
                }
            }
        }

        private void ShowNotificationOverlay(string copiedValue, bool isPubChem = false)
        {
            string text = isPubChem ? $"Copied: {copiedValue} g/mol (PubChem)" : $"Copied: {copiedValue} g/mol";
            
            // Create notification window
            var notificationWindow = new Window
            {
                Title = "Copy Notification",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = System.Windows.Media.Brushes.Transparent,
                AllowsTransparency = true,
                Topmost = true,
                Opacity = 1
            };
            
            // Add ScaleTransform for animations
            notificationWindow.RenderTransform = new ScaleTransform(1, 1);
            notificationWindow.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5); // Scale from center
            
            // Create the notification content with Monokai styling
            var border = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(12, 8, 12, 8),
                Background = SafeBrushFromString(settings.NotificationBackgroundColor, "#FF57584F"),
                BorderBrush = SafeBrushFromString(settings.NotificationBorderColor, "#FFFFFFFF"),
                BorderThickness = new Thickness(1)
            };
            
            // Create content with checkmark and text
            var stackPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };
            
            var checkmark = new TextBlock
            {
                Text = "✓",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = SafeBrushFromString(settings.NotificationTextColor, "#FFF8F8F2"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = SafeBrushFromString(settings.NotificationTextColor, "#FFF8F8F2"),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            stackPanel.Children.Add(checkmark);
            stackPanel.Children.Add(textBlock);
            border.Child = stackPanel;
            notificationWindow.Content = border;
            
            // Show the window
            notificationWindow.Show();
            
            // Create animations
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                BeginTime = TimeSpan.FromMilliseconds(600)
            };
            
            var scaleOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.9,
                Duration = TimeSpan.FromMilliseconds(150),
                BeginTime = TimeSpan.FromMilliseconds(600)
            };
            
            // Create storyboard
            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeOut);
            storyboard.Children.Add(scaleOut);
            
            // Set targets
            Storyboard.SetTarget(fadeOut, notificationWindow);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            
            Storyboard.SetTarget(scaleOut, notificationWindow);
            Storyboard.SetTargetProperty(scaleOut, new PropertyPath("RenderTransform.ScaleX"));
            
            var scaleOutY = new DoubleAnimation
            {
                From = 1.0,
                To = 0.9,
                Duration = TimeSpan.FromMilliseconds(150),
                BeginTime = TimeSpan.FromMilliseconds(600)
            };
            Storyboard.SetTarget(scaleOutY, notificationWindow);
            Storyboard.SetTargetProperty(scaleOutY, new PropertyPath("RenderTransform.ScaleY"));
            storyboard.Children.Add(scaleOutY);
            
            // Handle completion
            storyboard.Completed += (s, e) =>
            {
                notificationWindow.Close();
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
                StructureImage.Visibility = Visibility.Collapsed; // Hide image when no search
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
                    StructureImage.Visibility = Visibility.Collapsed; // Hide image for local parsing
                }
                else
                {
                    lastMolarMass = null;
                    isPubChemResult = false;
                    ResultsTextBlock.Text = $"Local parsing failed: {mmResult.Error}";
                    StructureImage.Visibility = Visibility.Collapsed;
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
                    StructureImage.Visibility = Visibility.Collapsed;
                }
                return;
            }

            // Default behavior: try local parsing first, then PubChem
            var localResult = SmartFormulaParser.ParseFormula(query);
            
            // Debug: Show parsing result for "ohge"
            if (query.ToLower() == "ohge")
            {
                var debugInfo = SmartFormulaParser.DebugParseResult(query);
                ResultsTextBlock.Text = $"Debug for '{query}': {debugInfo}";
                return;
            }
            

            
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
                StructureImage.Visibility = Visibility.Collapsed; // Hide image for local parsing
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
                    StructureImage.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async Task HandlePubChemResult(PubChemSearchResult pubChemResult)
        {
            var compound = pubChemResult.Compound;
            lastMolarMass = compound.MolecularWeight;
            isPubChemResult = true;
            lastPubChemCID = compound.CID; // Store CID for Ctrl+Enter
            
            var sb = new StringBuilder();
            sb.AppendLine($"PubChem Search: {pubChemResult.SearchTerm}");
            sb.AppendLine($"CID: {compound.CID}");
            
            if (!string.IsNullOrEmpty(compound.Synonym))
                sb.AppendLine($"Name: {compound.Synonym}");
            
            if (!string.IsNullOrEmpty(compound.MolecularFormula))
                sb.AppendLine($"Formula: {compound.MolecularFormula}");
            
            // Format molecular weight with proper decimal places
            string format = $"F{settings.DecimalPlaces}";
            sb.AppendLine($"Molecular Weight: {compound.MolecularWeight.ToString(format)} g/mol");
            
            ResultsTextBlock.Text = sb.ToString().TrimEnd('\n', '\r');
            
            // Load and display 2D structure image if available
            if (!string.IsNullOrEmpty(compound.Structure2DUrl))
            {
                await LoadStructureImageAsync(compound.Structure2DUrl);
            }
            else
            {
                StructureImage.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadStructureImageAsync(string imageUrl)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                
                using var stream = new MemoryStream(imageBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // Important for cross-thread usage
                
                Dispatcher.Invoke(() =>
                {
                    StructureImage.Source = bitmap;
                    StructureImage.Visibility = Visibility.Visible;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load structure image: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    StructureImage.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void ApplyWindowScaling()
        {
            var scale = settings.WindowScale;
            var mainBorder = this.Content as Border;
            if (mainBorder != null)
            {
                if (scale != 1.0)
                {
                    mainBorder.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
                    this.Width = 420 * scale;
                    this.MinWidth = 420 * scale;
                }
                else
                {
                    mainBorder.LayoutTransform = null;
                    this.Width = 420;
                    this.MinWidth = 420;
                }
            }
        }

        private System.Windows.Media.Brush SafeBrushFromString(string colorString, string fallback)
        {
            var brushConverter = new System.Windows.Media.BrushConverter();
            try
            {
                return (System.Windows.Media.Brush)brushConverter.ConvertFromString(colorString);
            }
            catch
            {
                return (System.Windows.Media.Brush)brushConverter.ConvertFromString(fallback);
            }
        }
    }
} 