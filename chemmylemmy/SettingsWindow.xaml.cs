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
        }

        private void StopHotkeyRecording()
        {
            isRecordingHotkey = false;
            keyCheckTimer.Stop();
            
            this.PreviewKeyDown -= SettingsWindow_PreviewKeyDown;
            this.PreviewKeyUp -= SettingsWindow_PreviewKeyUp;
            
            ChangeHotkeyButton.Content = "Change";
            ChangeHotkeyButton.Background = System.Windows.Media.Brushes.White;
        }

        private void SettingsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!isRecordingHotkey) return;
            
            e.Handled = true;
            
            // Use timer to check keyboard state after a brief delay
            keyCheckTimer.Stop();
            keyCheckTimer.Start();
        }

        private void SettingsWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!isRecordingHotkey) return;
            e.Handled = true;
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to reset all settings to defaults?", 
                                       "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                settings = new Settings();
                LoadSettings();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Save settings
            if (DecimalPlacesComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (int.TryParse(selectedItem.Content.ToString(), out int decimalPlaces))
                {
                    settings.DecimalPlaces = decimalPlaces;
                }
            }
            
            settings.AutoHideOnFocusLoss = AutoHideCheckBox.IsChecked ?? true;
            settings.ShowCopyConfirmation = ShowCopyConfirmationCheckBox.IsChecked ?? false;
            
            settings.Save();
            
            // Notify main window about settings change
            mainWindow.OnSettingsChanged(settings);
            
            // Don't show notification and don't close - just trust it worked
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (isRecordingHotkey)
                {
                    StopHotkeyRecording();
                }
                else
                {
                    Close();
                }
            }
            base.OnKeyDown(e);
        }
    }
} 