using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.Windows.Forms;
using System.Drawing;
using WinFormsApplication = System.Windows.Forms.Application;
using WpfApplication = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace chemmylemmy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private Settings settings;

        public MainWindow()
        {
            InitializeComponent();
            
            // Load settings
            settings = Settings.Load();
            
            // Register hotkey based on settings
            RegisterHotkey();
            
            // Setup system tray icon
            SetupSystemTray();
        }

        private void RegisterHotkey()
        {
            try
            {
                // Remove existing hotkey if any
                HotkeyManager.Current.Remove("ShowSearchBar");
                
                // Register new hotkey from settings
                HotkeyManager.Current.AddOrReplace("ShowSearchBar", settings.HotkeyKey, settings.HotkeyModifiers, OnShowSearchBarHotkey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering hotkey: {ex.Message}");
            }
        }

        private void SetupSystemTray()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application; // You can replace this with a custom icon
            notifyIcon.Text = $"ChemmyLemmy - Press {settings.GetHotkeyDisplayString()} to search";
            notifyIcon.Visible = true;

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add($"Show Search ({settings.GetHotkeyDisplayString()})", null, (s, e) => ShowSearchBarWindow());
            contextMenu.Items.Add("-"); // Separator
            contextMenu.Items.Add("Settings", null, (s, e) => ShowSettingsWindow());
            contextMenu.Items.Add("-"); // Separator
            contextMenu.Items.Add("Exit", null, (s, e) => WpfApplication.Current.Shutdown());
            
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => ShowSearchBarWindow();
        }

        private void OnShowSearchBarHotkey(object sender, HotkeyEventArgs e)
        {
            ShowSearchBarWindow();
            e.Handled = true;
        }

        public void OnSettingsChanged(Settings newSettings)
        {
            settings = newSettings;
            
            // Update hotkey
            RegisterHotkey();
            
            // Update system tray tooltip
            if (notifyIcon != null)
            {
                notifyIcon.Text = $"ChemmyLemmy - Press {settings.GetHotkeyDisplayString()} to search";
                
                // Update context menu
                var contextMenu = notifyIcon.ContextMenuStrip;
                if (contextMenu.Items.Count > 0)
                {
                    contextMenu.Items[0].Text = $"Show Search ({settings.GetHotkeyDisplayString()})";
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            HotkeyManager.Current.Remove("ShowSearchBar");
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            base.OnClosed(e);
        }

        public void ShowSearchBarWindow()
        {
            var searchBar = new SearchBarWindow(settings);
            searchBar.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            searchBar.Topmost = true;
            searchBar.Show();
            searchBar.Activate();
        }

        public void ShowSettingsWindow()
        {
            var settingsWindow = new SettingsWindow(this);
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            settingsWindow.Show();
        }

        public void ApplySettings()
        {
            // Reload settings
            settings = Settings.Load();
            
            // Update hotkey
            RegisterHotkey();
            
            // Update system tray tooltip
            if (notifyIcon != null)
            {
                notifyIcon.Text = $"ChemmyLemmy - Press {settings.GetHotkeyDisplayString()} to search";
                
                // Update context menu
                var contextMenu = notifyIcon.ContextMenuStrip;
                if (contextMenu.Items.Count > 0)
                {
                    contextMenu.Items[0].Text = $"Show Search ({settings.GetHotkeyDisplayString()})";
                }
            }
        }
    }
}