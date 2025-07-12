using System;
using System.Windows;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfApplication = System.Windows.Application;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows.Media;
using System.Globalization;

namespace chemmylemmy
{
    public partial class SettingsWindow : Window
    {
        private Settings settings;
        private bool isRecordingHotkey = false;
        private MainWindow mainWindow;
        private DispatcherTimer keyCheckTimer;
        private bool isLoadingSettings = false;

        private void UpdateThemeResources()
        {
            var currentSettings = Settings.Load();
            
            // Update the resource brushes
            var backgroundBrush = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF2A2A2A");
            var borderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
            var textBrush = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
            var highlightBrush = SafeBrushFromString(currentSettings.HighlightColor, "#FFA6E22E");
            
            this.Resources["ThemeBackgroundBrush"] = backgroundBrush;
            this.Resources["ThemeBorderBrush"] = borderBrush;
            this.Resources["ThemeTextBrush"] = textBrush;
            this.Resources["ThemeHighlightBrush"] = highlightBrush;
            
            // Force the tab control to update its styles
            if (MainTabControl != null)
            {
                MainTabControl.InvalidateVisual();
                MainTabControl.UpdateLayout();
            }
        }

        public SettingsWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            InitializeComponent();
            settings = Settings.Load();
            LoadSettings();
            
            // Initialize timer for key state checking
            keyCheckTimer = new DispatcherTimer();
            keyCheckTimer.Interval = TimeSpan.FromMilliseconds(0);
            keyCheckTimer.Tick += KeyCheckTimer_Tick;
            
            // Apply theme resources immediately
            UpdateThemeResources();
            
            // Apply theme colors after the window is fully loaded
            this.Loaded += (s, e) => ApplyThemeColors();
        }



        private void KeyCheckTimer_Tick(object sender, EventArgs e)
        {
            keyCheckTimer.Stop();
            
            if (!isRecordingHotkey) return;
            
            // Check current keyboard state
            var modifiers = ModifierKeys.None;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                modifiers |= ModifierKeys.Control;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                modifiers |= ModifierKeys.Shift;
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                modifiers |= ModifierKeys.Alt;
            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
                modifiers |= ModifierKeys.Windows;
            
            // Check for main keys
            Key mainKey = Key.None;
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (key != Key.LeftCtrl && key != Key.RightCtrl &&
                    key != Key.LeftShift && key != Key.RightShift &&
                    key != Key.LeftAlt && key != Key.RightAlt &&
                    key != Key.LWin && key != Key.RWin &&
                    key != Key.System &&
                    key != Key.None &&
                    Keyboard.IsKeyDown(key))
                {
                    mainKey = key;
                    break;
                }
            }
            
            // Update display
            if (mainKey != Key.None && modifiers != ModifierKeys.None)
            {
                // We have a complete key combination
                settings.HotkeyKey = mainKey;
                settings.HotkeyModifiers = modifiers;
                HotkeyTextBox.Text = settings.GetHotkeyDisplayString();
                
                // Auto-stop recording when we get a complete combination
                StopHotkeyRecording();
            }
            else if (modifiers != ModifierKeys.None)
            {
                // Show modifiers while waiting for main key
                var modifierList = new List<string>();
                if ((modifiers & ModifierKeys.Control) != 0) modifierList.Add("Ctrl");
                if ((modifiers & ModifierKeys.Shift) != 0) modifierList.Add("Shift");
                if ((modifiers & ModifierKeys.Alt) != 0) modifierList.Add("Alt");
                if ((modifiers & ModifierKeys.Windows) != 0) modifierList.Add("Win");
                HotkeyTextBox.Text = string.Join("+", modifierList) + "+...";
            }
        }

        private void LoadSettings()
        {
            isLoadingSettings = true;
            
            // Load current settings
            HotkeyTextBox.Text = settings.GetHotkeyDisplayString();
            
            // Set decimal places combo box
            switch (settings.DecimalPlaces)
            {
                case 2:
                    DecimalPlacesComboBox.SelectedIndex = 0;
                    break;
                case 3:
                    DecimalPlacesComboBox.SelectedIndex = 1;
                    break;
                case 4:
                    DecimalPlacesComboBox.SelectedIndex = 2;
                    break;
                case 5:
                    DecimalPlacesComboBox.SelectedIndex = 3;
                    break;
                default:
                    DecimalPlacesComboBox.SelectedIndex = 1; // Default to 3
                    break;
            }
            
            AutoHideCheckBox.IsChecked = settings.AutoHideOnFocusLoss;
            ShowCopyConfirmationCheckBox.IsChecked = settings.ShowCopyConfirmation;
            
            // Set scale combo box
            switch (settings.WindowScale)
            {
                case 1.0:
                    ScaleComboBox.SelectedIndex = 0; // 100%
                    break;
                case 1.25:
                    ScaleComboBox.SelectedIndex = 1; // 125%
                    break;
                case 1.5:
                    ScaleComboBox.SelectedIndex = 2; // 150%
                    break;
                case 1.75:
                    ScaleComboBox.SelectedIndex = 3; // 175%
                    break;
                case 2.0:
                    ScaleComboBox.SelectedIndex = 4; // 200%
                    break;
                default:
                    ScaleComboBox.SelectedIndex = 0; // Default to 100%
                    break;
            }

            // Load color settings
            SearchBoxBorderColorTextBox.Text = settings.SearchBoxBorderColor;
            SearchBoxTextColorTextBox.Text = settings.SearchBoxTextColor;
            SearchBoxBackgroundColorTextBox.Text = settings.SearchBoxBackgroundColor;
            ResultsBoxBorderColorTextBox.Text = settings.ResultsBoxBorderColor;
            ResultsBoxTextColorTextBox.Text = settings.ResultsBoxTextColor;
            ResultsBoxBackgroundColorTextBox.Text = settings.ResultsBoxBackgroundColor;
            WindowBorderColorTextBox.Text = settings.WindowBorderColor;
            WindowBackgroundColorTextBox.Text = settings.WindowBackgroundColor;
            HighlightColorTextBox.Text = settings.HighlightColor;
            
            // Load notification color settings
            NotificationBackgroundColorTextBox.Text = settings.NotificationBackgroundColor;
            NotificationBorderColorTextBox.Text = settings.NotificationBorderColor;
            NotificationTextColorTextBox.Text = settings.NotificationTextColor;
            
            // Load preset names
            Preset1NameTextBox.Text = settings.Preset1.Name;
            Preset2NameTextBox.Text = settings.Preset2.Name;
            Preset3NameTextBox.Text = settings.Preset3.Name;
            
            isLoadingSettings = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecordingHotkey)
            {
                StartHotkeyRecording();
            }
            else
            {
                StopHotkeyRecording();
            }
        }

        private void StartHotkeyRecording()
        {
            isRecordingHotkey = true;
            
            HotkeyTextBox.Text = "Press keys...";
            ChangeHotkeyButton.Content = "Stop Recording";
            ChangeHotkeyButton.Background = System.Windows.Media.Brushes.Red;
            
            // Capture all keyboard input
            this.PreviewKeyDown += SettingsWindow_PreviewKeyDown;
            this.PreviewKeyUp += SettingsWindow_PreviewKeyUp;
            
            // Start the timer to check for key combinations
            keyCheckTimer.Start();
        }

        private void StopHotkeyRecording()
        {
            isRecordingHotkey = false;
            
            ChangeHotkeyButton.Content = "Change";
            ChangeHotkeyButton.Background = System.Windows.Media.Brushes.Green;
            
            // Stop capturing keyboard input
            this.PreviewKeyDown -= SettingsWindow_PreviewKeyDown;
            this.PreviewKeyUp -= SettingsWindow_PreviewKeyUp;
            
            keyCheckTimer.Stop();
        }

        private void SettingsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            keyCheckTimer.Start();
        }

        private void SettingsWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            keyCheckTimer.Start();
        }

        private void MonokaiTheme_Click(object sender, RoutedEventArgs e)
        {
            // Monokai theme colors
            SearchBoxBorderColorTextBox.Text = "#FF49483E";
            SearchBoxTextColorTextBox.Text = "#FFF8F8F2";
            SearchBoxBackgroundColorTextBox.Text = "#FF57584F";
            ResultsBoxBorderColorTextBox.Text = "#FF49483E";
            ResultsBoxTextColorTextBox.Text = "#FFF8F8F2";
            ResultsBoxBackgroundColorTextBox.Text = "#FF35362F";
            WindowBorderColorTextBox.Text = "#FF49483E";
            WindowBackgroundColorTextBox.Text = "#FF272822";
            HighlightColorTextBox.Text = "#FFA6E22E";
            NotificationBackgroundColorTextBox.Text = "#FF57584F";
            NotificationBorderColorTextBox.Text = "#FF49483E";
            NotificationTextColorTextBox.Text = "#FFF8F8F2";
            
            // Apply the new theme colors to the settings window
            ApplyThemeColors();
        }

        private void DraculaTheme_Click(object sender, RoutedEventArgs e)
        {
            // Dracula theme colors
            SearchBoxBorderColorTextBox.Text = "#FF6272A4";
            SearchBoxTextColorTextBox.Text = "#FFF8F8F2";
            SearchBoxBackgroundColorTextBox.Text = "#FF44475A";
            ResultsBoxBorderColorTextBox.Text = "#FF6272A4";
            ResultsBoxTextColorTextBox.Text = "#FFF8F8F2";
            ResultsBoxBackgroundColorTextBox.Text = "#FF282A36";
            WindowBorderColorTextBox.Text = "#FF6272A4";
            WindowBackgroundColorTextBox.Text = "#FF282A36";
            HighlightColorTextBox.Text = "#FFBD93F9"; // purple-ish
            NotificationBackgroundColorTextBox.Text = "#FF44475A";
            NotificationBorderColorTextBox.Text = "#FF6272A4";
            NotificationTextColorTextBox.Text = "#FFF8F8F2";
            
            // Apply the new theme colors to the settings window
            ApplyThemeColors();
        }

        private void SolarizedTheme_Click(object sender, RoutedEventArgs e)
        {
            // Solarized theme colors
            SearchBoxBorderColorTextBox.Text = "#FF586E75";
            SearchBoxTextColorTextBox.Text = "#FFFDF6E3"; // brightened text
            SearchBoxBackgroundColorTextBox.Text = "#FF073642";
            ResultsBoxBorderColorTextBox.Text = "#FF586E75";
            ResultsBoxTextColorTextBox.Text = "#FFFDF6E3"; // brightened text
            ResultsBoxBackgroundColorTextBox.Text = "#FF002B36";
            WindowBorderColorTextBox.Text = "#FF586E75";
            WindowBackgroundColorTextBox.Text = "#FF002B36";
            HighlightColorTextBox.Text = "#FFECB464"; // yellow-orange
            NotificationBackgroundColorTextBox.Text = "#FF073642";
            NotificationBorderColorTextBox.Text = "#FF586E75";
            NotificationTextColorTextBox.Text = "#FFFDF6E3"; // brightened text
            
            // Apply the new theme colors to the settings window
            ApplyThemeColors();
        }

        private void NordTheme_Click(object sender, RoutedEventArgs e)
        {
            // Nord theme colors
            SearchBoxBorderColorTextBox.Text = "#FF4C566A";
            SearchBoxTextColorTextBox.Text = "#FFECEFF4";
            SearchBoxBackgroundColorTextBox.Text = "#FF3B4252";
            ResultsBoxBorderColorTextBox.Text = "#FF4C566A";
            ResultsBoxTextColorTextBox.Text = "#FFECEFF4";
            ResultsBoxBackgroundColorTextBox.Text = "#FF2E3440";
            WindowBorderColorTextBox.Text = "#FF4C566A";
            WindowBackgroundColorTextBox.Text = "#FF2E3440";
            HighlightColorTextBox.Text = "#FFA3BE8C";
            NotificationBackgroundColorTextBox.Text = "#FF3B4252";
            NotificationBorderColorTextBox.Text = "#FF4C566A";
            NotificationTextColorTextBox.Text = "#FFECEFF4";
            
            // Apply the new theme colors to the settings window
            ApplyThemeColors();
        }

        private void GruvboxTheme_Click(object sender, RoutedEventArgs e)
        {
            // Gruvbox theme colors
            SearchBoxBorderColorTextBox.Text = "#FF504945";
            SearchBoxTextColorTextBox.Text = "#FFEBDBB2";
            SearchBoxBackgroundColorTextBox.Text = "#FF3C3836";
            ResultsBoxBorderColorTextBox.Text = "#FF504945";
            ResultsBoxTextColorTextBox.Text = "#FFEBDBB2";
            ResultsBoxBackgroundColorTextBox.Text = "#FF282828";
            WindowBorderColorTextBox.Text = "#FF504945";
            WindowBackgroundColorTextBox.Text = "#FF282828";
            HighlightColorTextBox.Text = "#FFB8BB26";
            NotificationBackgroundColorTextBox.Text = "#FF3C3836";
            NotificationBorderColorTextBox.Text = "#FF504945";
            NotificationTextColorTextBox.Text = "#FFEBDBB2";
            
            // Apply the new theme colors to the settings window
            ApplyThemeColors();
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            // Reset to Monokai defaults
            MonokaiTheme_Click(sender, e);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Save decimal places
            switch (DecimalPlacesComboBox.SelectedIndex)
            {
                case 0:
                    settings.DecimalPlaces = 2;
                    break;
                case 1:
                    settings.DecimalPlaces = 3;
                    break;
                case 2:
                    settings.DecimalPlaces = 4;
                    break;
                case 3:
                    settings.DecimalPlaces = 5;
                    break;
            }
            
            // Save window scale
            switch (ScaleComboBox.SelectedIndex)
            {
                case 0:
                    settings.WindowScale = 1.0;
                    break;
                case 1:
                    settings.WindowScale = 1.25;
                    break;
                case 2:
                    settings.WindowScale = 1.5;
                    break;
                case 3:
                    settings.WindowScale = 1.75;
                    break;
                case 4:
                    settings.WindowScale = 2.0;
                    break;
            }
            
            // Save checkboxes
            settings.AutoHideOnFocusLoss = AutoHideCheckBox.IsChecked ?? true;
            settings.ShowCopyConfirmation = ShowCopyConfirmationCheckBox.IsChecked ?? true;
            
            // Save color settings
            settings.SearchBoxBorderColor = SearchBoxBorderColorTextBox.Text;
            settings.SearchBoxTextColor = SearchBoxTextColorTextBox.Text;
            settings.SearchBoxBackgroundColor = SearchBoxBackgroundColorTextBox.Text;
            settings.ResultsBoxBorderColor = ResultsBoxBorderColorTextBox.Text;
            settings.ResultsBoxTextColor = ResultsBoxTextColorTextBox.Text;
            settings.ResultsBoxBackgroundColor = ResultsBoxBackgroundColorTextBox.Text;
            settings.WindowBorderColor = WindowBorderColorTextBox.Text;
            settings.WindowBackgroundColor = WindowBackgroundColorTextBox.Text;
            settings.HighlightColor = HighlightColorTextBox.Text;
            
            // Save notification color settings
            settings.NotificationBackgroundColor = NotificationBackgroundColorTextBox.Text;
            settings.NotificationBorderColor = NotificationBorderColorTextBox.Text;
            settings.NotificationTextColor = NotificationTextColorTextBox.Text;
            
            // Save settings
            settings.Save();
            
            // Update main window
            mainWindow.ApplySettings();
            
            // Apply the new theme colors to the settings window
            Dispatcher.Invoke(new Action(() => ApplyThemeColors()), System.Windows.Threading.DispatcherPriority.Loaded);
            
            // Show success message
            System.Windows.MessageBox.Show("Settings saved successfully!", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        private void SavePreset1_Click(object sender, RoutedEventArgs e)
        {
            settings.Preset1.CopyFrom(settings);
            settings.Preset1.Name = Preset1NameTextBox.Text;
            settings.Save();
            System.Windows.MessageBox.Show("Preset 1 saved successfully!", "Preset Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadPreset1_Click(object sender, RoutedEventArgs e)
        {
            if (!settings.Preset1.IsEmpty)
            {
                settings.Preset1.ApplyTo(settings);
                LoadSettings();
                // Apply theme colors after UI is updated
                Dispatcher.Invoke(new Action(() => ApplyThemeColors()), System.Windows.Threading.DispatcherPriority.Loaded);
                System.Windows.MessageBox.Show("Preset 1 loaded successfully!", "Preset Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Preset 1 is empty. Save a theme first.", "Empty Preset", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SavePreset2_Click(object sender, RoutedEventArgs e)
        {
            settings.Preset2.CopyFrom(settings);
            settings.Preset2.Name = Preset2NameTextBox.Text;
            settings.Save();
            System.Windows.MessageBox.Show("Preset 2 saved successfully!", "Preset Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadPreset2_Click(object sender, RoutedEventArgs e)
        {
            if (!settings.Preset2.IsEmpty)
            {
                settings.Preset2.ApplyTo(settings);
                LoadSettings();
                // Apply theme colors after UI is updated
                Dispatcher.Invoke(new Action(() => ApplyThemeColors()), System.Windows.Threading.DispatcherPriority.Loaded);
                System.Windows.MessageBox.Show("Preset 2 loaded successfully!", "Preset Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Preset 2 is empty. Save a theme first.", "Empty Preset", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SavePreset3_Click(object sender, RoutedEventArgs e)
        {
            settings.Preset3.CopyFrom(settings);
            settings.Preset3.Name = Preset3NameTextBox.Text;
            settings.Save();
            System.Windows.MessageBox.Show("Preset 3 saved successfully!", "Preset Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadPreset3_Click(object sender, RoutedEventArgs e)
        {
            if (!settings.Preset3.IsEmpty)
            {
                settings.Preset3.ApplyTo(settings);
                LoadSettings();
                // Apply theme colors after UI is updated
                Dispatcher.Invoke(new Action(() => ApplyThemeColors()), System.Windows.Threading.DispatcherPriority.Loaded);
                System.Windows.MessageBox.Show("Preset 3 loaded successfully!", "Preset Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Preset 3 is empty. Save a theme first.", "Empty Preset", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyThemeColors()
        {
            // Apply current theme colors to the settings window
            var currentSettings = Settings.Load();
            
            // Update the resource brushes first
            UpdateThemeResources();
            
            // Window background and border
            this.Background = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF222222");
            
            // Title bar
            var titleBar = this.FindName("TitleBar") as Grid;
            if (titleBar != null)
            {
                titleBar.Background = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF333333");
            }
            
            // Tab control styling
            if (MainTabControl != null)
            {
                MainTabControl.Background = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF222222");
            }
            
            // Apply colors to bottom buttons (outside tab content)
            ApplyColorsToBottomButtons(currentSettings);
            

            
            // Force all tabs to be rendered and apply colors to all elements
            ForceRenderAllTabsAndApplyColors(currentSettings);
        }

        private void ApplyColorsToBottomButtons(Settings currentSettings)
        {
            // Apply colors to the bottom buttons (Save, Reset Defaults, Cancel)
            if (SaveButton != null)
            {
                SaveButton.Background = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                SaveButton.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                SaveButton.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
            }
            
            if (ResetButton != null)
            {
                ResetButton.Background = System.Windows.Media.Brushes.Transparent;
                ResetButton.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                ResetButton.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
            }
            
            if (CancelButton != null)
            {
                CancelButton.Background = System.Windows.Media.Brushes.Transparent;
                CancelButton.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                CancelButton.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
            }
        }

        private void ForceRenderAllTabsAndApplyColors(Settings currentSettings)
        {
            if (MainTabControl != null && MainTabControl.Items.Count > 0)
            {
                var originalSelectedIndex = MainTabControl.SelectedIndex;
                
                // Temporarily switch to each tab to ensure they're rendered and apply colors
                for (int i = 0; i < MainTabControl.Items.Count; i++)
                {
                    MainTabControl.SelectedIndex = i;
                    MainTabControl.UpdateLayout();
                    
                    // Apply colors to the current tab's content
                    ApplyColorsToCurrentTab(currentSettings);
                }
                
                // Restore original selection
                MainTabControl.SelectedIndex = originalSelectedIndex;
                MainTabControl.UpdateLayout();
            }
        }

        private void ApplyColorsToCurrentTab(Settings currentSettings)
        {
            // Find the current tab's content
            var currentTabItem = MainTabControl.SelectedItem as TabItem;
            if (currentTabItem?.Content is FrameworkElement content)
            {
                // Apply colors to all Border elements in the current tab
                var borders = FindVisualChildren<System.Windows.Controls.Border>(content);
                foreach (var border in borders)
                {
                    // Handle anonymous borders with CornerRadius="5" (the section backgrounds)
                    if (string.IsNullOrEmpty(border.Name) && 
                        border.CornerRadius.TopLeft == 5 && border.CornerRadius.TopRight == 5 && 
                        border.CornerRadius.BottomLeft == 5 && border.CornerRadius.BottomRight == 5)
                    {
                        border.Background = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF2A2A2A");
                        border.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                    }
                }
                
                // Apply colors to all TextBox elements in the current tab
                var textBoxes = FindVisualChildren<System.Windows.Controls.TextBox>(content);
                foreach (var textBox in textBoxes)
                {
                    textBox.Background = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF333333");
                    textBox.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                    textBox.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                }
                
                // Apply colors to all ComboBox elements in the current tab
                var comboBoxes = FindVisualChildren<System.Windows.Controls.ComboBox>(content);
                foreach (var comboBox in comboBoxes)
                {
                    comboBox.Background = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF333333");
                    comboBox.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                    comboBox.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                }
                
                // Apply colors to all Button elements in the current tab
                var buttons = FindVisualChildren<System.Windows.Controls.Button>(content);
                foreach (var button in buttons)
                {
                    if (button.Name == "CloseButton")
                    {
                        button.Background = System.Windows.Media.Brushes.Transparent;
                        button.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                    }
                    else if (button.Content?.ToString() == "Save")
                    {
                        button.Background = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                        button.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                        button.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                    }
                    else
                    {
                        button.Background = System.Windows.Media.Brushes.Transparent;
                        button.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                        button.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                    }
                }
                
                // Apply colors to all TextBlock elements in the current tab
                var textBlocks = FindVisualChildren<System.Windows.Controls.TextBlock>(content);
                foreach (var textBlock in textBlocks)
                {
                    textBlock.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                }
                
                // Apply colors to all CheckBox elements in the current tab
                var checkBoxes = FindVisualChildren<System.Windows.Controls.CheckBox>(content);
                foreach (var checkBox in checkBoxes)
                {
                    checkBox.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                }
            }
        }



        private void ForceRenderAllTabs()
        {
            // Force all tabs to be rendered by temporarily switching to each tab
            if (MainTabControl != null && MainTabControl.Items.Count > 0)
            {
                var originalSelectedIndex = MainTabControl.SelectedIndex;
                
                // Temporarily switch to each tab to ensure they're rendered
                for (int i = 0; i < MainTabControl.Items.Count; i++)
                {
                    MainTabControl.SelectedIndex = i;
                    MainTabControl.UpdateLayout();
                }
                
                // Restore original selection
                MainTabControl.SelectedIndex = originalSelectedIndex;
                MainTabControl.UpdateLayout();
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var childResult = FindVisualChild<T>(child);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        private void ApplyColorsToElements(Settings currentSettings)
        {
            // Apply colors to all Border elements in the settings window
            var borders = FindVisualChildren<System.Windows.Controls.Border>(this);
            foreach (var border in borders)
            {
                // Handle named borders
                if (!string.IsNullOrEmpty(border.Name))
                {
                    if (border.Name.Contains("Border"))
                    {
                        {
                            border.Background = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF2A2A2A");
                            border.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                        }
                    }
                }
                // Handle anonymous borders with CornerRadius="5" (the section backgrounds)
                else if (border.CornerRadius.TopLeft == 5 && border.CornerRadius.TopRight == 5 && 
                         border.CornerRadius.BottomLeft == 5 && border.CornerRadius.BottomRight == 5)
                {
                    border.Background = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF2A2A2A");
                    border.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                }
            }
            
            // Apply colors to all TextBox elements
            var textBoxes = FindVisualChildren<System.Windows.Controls.TextBox>(this);
            foreach (var textBox in textBoxes)
            {
                textBox.Background = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF333333");
                textBox.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                textBox.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
            }
            
            // Apply colors to all ComboBox elements
            var comboBoxes = FindVisualChildren<System.Windows.Controls.ComboBox>(this);
            foreach (var comboBox in comboBoxes)
            {
                comboBox.Background = SafeBrushFromString(currentSettings.WindowBackgroundColor, "#FF333333");
                comboBox.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                comboBox.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
            }
            
            // Apply colors to all Button elements
            var buttons = FindVisualChildren<System.Windows.Controls.Button>(this);
            foreach (var button in buttons)
            {
                if (button.Name == "CloseButton")
                {
                    button.Background = System.Windows.Media.Brushes.Transparent;
                    button.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                }
                else if (button.Content?.ToString() == "Save")
                {
                    button.Background = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                    button.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                    button.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                }
                else
                {
                    button.Background = System.Windows.Media.Brushes.Transparent;
                    button.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                    button.BorderBrush = SafeBrushFromString(currentSettings.WindowBorderColor, "#FF444444");
                }
            }
            
            // Apply colors to all TextBlock elements
            var textBlocks = FindVisualChildren<System.Windows.Controls.TextBlock>(this);
            foreach (var textBlock in textBlocks)
            {
                textBlock.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
            }
            
            // Apply colors to all CheckBox elements
            var checkBoxes = FindVisualChildren<System.Windows.Controls.CheckBox>(this);
            foreach (var checkBox in checkBoxes)
            {
                checkBox.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
            }
            
            // Apply colors to all TabItem elements
            var tabItems = FindVisualChildren<System.Windows.Controls.TabItem>(this);
            foreach (var tabItem in tabItems)
            {
                tabItem.Foreground = SafeBrushFromString(currentSettings.SearchBoxTextColor, "#FFF0F0F0");
                tabItem.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private System.Windows.Media.Brush SafeBrushFromString(string colorString, string fallbackColor)
        {
            try
            {
                var converter = new System.Windows.Media.BrushConverter();
                if (converter.ConvertFromString(colorString) is System.Windows.Media.Brush brush)
                {
                    return brush;
                }
            }
            catch
            {
                // If parsing fails, use fallback
            }
            
            try
            {
                var converter = new System.Windows.Media.BrushConverter();
                if (converter.ConvertFromString(fallbackColor) is System.Windows.Media.Brush fallback)
                {
                    return fallback;
                }
            }
            catch
            {
                // If even fallback fails, return a default brush
            }
            
            return System.Windows.Media.Brushes.Gray;
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                    yield return t;
                
                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }


    }
} 