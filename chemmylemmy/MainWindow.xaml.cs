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

namespace chemmylemmy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Register Ctrl+Shift+Z as the global hotkey
            HotkeyManager.Current.AddOrReplace("ShowSearchBar", Key.Z, ModifierKeys.Control | ModifierKeys.Shift, OnShowSearchBarHotkey);
        }

        private void OnShowSearchBarHotkey(object sender, HotkeyEventArgs e)
        {
            ShowSearchBarWindow();
            e.Handled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            HotkeyManager.Current.Remove("ShowSearchBar");
            base.OnClosed(e);
        }

        public void ShowSearchBarWindow()
        {
            var searchBar = new SearchBarWindow();
            searchBar.Owner = this;
            searchBar.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            searchBar.Show();
        }
    }
}