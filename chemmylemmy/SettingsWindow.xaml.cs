using System;
using System.Windows;
using System.Windows.Input;
using NHotkey;
using NHotkey.Wpf;

namespace chemmylemmy
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load current settings (placeholder for now)
            // In a real app, you'd load from a config file
            HotkeyTextBox.Text = "Ctrl+Shift+Z";
            DecimalPlacesComboBox.SelectedIndex = 1; // 3 decimal places
            AutoHideCheckBox.IsChecked = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement hotkey change dialog
            System.Windows.MessageBox.Show("Hotkey change feature coming soon!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to reset all settings to defaults?", 
                                       "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                HotkeyTextBox.Text = "Ctrl+Shift+Z";
                DecimalPlacesComboBox.SelectedIndex = 1;
                AutoHideCheckBox.IsChecked = true;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Save settings to config file
            System.Windows.MessageBox.Show("Settings saved!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            base.OnKeyDown(e);
        }
    }
} 