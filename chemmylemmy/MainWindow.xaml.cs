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

        public MainWindow()
        {
            InitializeComponent();
            
            // Register Ctrl+Shift+Z as the global hotkey
            HotkeyManager.Current.AddOrReplace("ShowSearchBar", Key.Z, ModifierKeys.Control | ModifierKeys.Shift, OnShowSearchBarHotkey);
            
            // Setup system tray icon
            SetupSystemTray();
        }

        private void SetupSystemTray()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application; // You can replace this with a custom icon
            notifyIcon.Text = "ChemmyLemmy - Press Ctrl+Shift+Z to search";
            notifyIcon.Visible = true;

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show Search (Ctrl+Shift+Z)", null, (s, e) => ShowSearchBarWindow());
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
            var searchBar = new SearchBarWindow();
            searchBar.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            searchBar.Topmost = true;
            searchBar.Show();
            searchBar.Activate();
        }
    }
}