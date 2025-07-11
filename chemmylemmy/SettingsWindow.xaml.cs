using System;
using System.Windows;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;
using System.Windows.Controls;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfApplication = System.Windows.Application;
using System.Collections.Generic;
using System.Windows.Threading;

namespace chemmylemmy
{
    public partial class SettingsWindow : Window
    {
        private Settings settings;
        private bool isRecordingHotkey = false;
        private MainWindow mainWindow;
        private DispatcherTimer keyCheckTimer;
        private bool isLoadingSettings = false;

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
                System.Windows.MessageBox.Show("Preset 3 loaded successfully!", "Preset Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Preset 3 is empty. Save a theme first.", "Empty Preset", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyThemeColors()
        {
            // Simple dark gray theme - no complex color matching
            // This will be replaced with proper theming later
        }


    }
} 