using System;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using NHotkey.Wpf;
using System.Collections.Generic;

namespace chemmylemmy
{
    public class Settings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChemmyLemmy",
            "settings.json");

        public int DecimalPlaces { get; set; } = 3;
        public Key HotkeyKey { get; set; } = Key.Z;
        public ModifierKeys HotkeyModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Shift;
        public bool AutoHideOnFocusLoss { get; set; } = true;
        public bool ShowCopyConfirmation { get; set; } = true;
        public double WindowScale { get; set; } = 1.0; // Window scaling factor

        // Color settings for UI customization
        public string SearchBoxBorderColor { get; set; } = "#FFFFFFFF"; // Accent (white)
        public string SearchBoxTextColor { get; set; } = "#FFF8F8F2"; // Monokai text
        public string SearchBoxBackgroundColor { get; set; } = "#FF57584F"; // SearchBoxBackgroundBrush
        public string ResultsBoxBorderColor { get; set; } = "#FF49483E"; // Monokai border
        public string ResultsBoxTextColor { get; set; } = "#FFF8F8F2"; // Monokai text
        public string ResultsBoxBackgroundColor { get; set; } = "#FF35362F"; // ResultsBoxBackgroundBrush
        public string WindowBorderColor { get; set; } = "#FF49483E"; // Monokai border
        public string WindowBackgroundColor { get; set; } = "#FF272822"; // Monokai background
        public string HighlightColor { get; set; } = "#FFA6E22E"; // Monokai highlight (green)

        // Notification color settings
        public string NotificationBackgroundColor { get; set; } = "#FF57584F"; // Same as search box
        public string NotificationBorderColor { get; set; } = "#FF49483E"; // Same as window border
        public string NotificationTextColor { get; set; } = "#FFF8F8F2"; // Same as search box

        // Color preset settings
        public ColorPreset Preset1 { get; set; } = new ColorPreset();
        public ColorPreset Preset2 { get; set; } = new ColorPreset();
        public ColorPreset Preset3 { get; set; } = new ColorPreset();

        // Built-in themes
        public static readonly ColorPreset MonokaiTheme = new ColorPreset
        {
            Name = "Monokai",
            SearchBoxBorderColor = "#FFFFFFFF",
            SearchBoxTextColor = "#FFF8F8F2",
            SearchBoxBackgroundColor = "#FF57584F",
            ResultsBoxBorderColor = "#FF49483E",
            ResultsBoxTextColor = "#FFF8F8F2",
            ResultsBoxBackgroundColor = "#FF35362F",
            WindowBorderColor = "#FF49483E",
            WindowBackgroundColor = "#FF272822",
            HighlightColor = "#FFA6E22E",
            NotificationBackgroundColor = "#FF57584F",
            NotificationBorderColor = "#FF49483E",
            NotificationTextColor = "#FFF8F8F2"
        };

        public static readonly ColorPreset DraculaTheme = new ColorPreset
        {
            Name = "Dracula",
            SearchBoxBorderColor = "#FF6272A4",
            SearchBoxTextColor = "#FFF8F8F2",
            SearchBoxBackgroundColor = "#FF44475A",
            ResultsBoxBorderColor = "#FF6272A4",
            ResultsBoxTextColor = "#FFF8F8F2",
            ResultsBoxBackgroundColor = "#FF282A36",
            WindowBorderColor = "#FF6272A4",
            WindowBackgroundColor = "#FF282A36",
            HighlightColor = "#FFFF79C6",
            NotificationBackgroundColor = "#FF44475A",
            NotificationBorderColor = "#FF6272A4",
            NotificationTextColor = "#FFF8F8F2"
        };

        public static readonly ColorPreset SolarizedDarkTheme = new ColorPreset
        {
            Name = "Solarized Dark",
            SearchBoxBorderColor = "#FF586E75",
            SearchBoxTextColor = "#FFE6E6E6",
            SearchBoxBackgroundColor = "#FF073642",
            ResultsBoxBorderColor = "#FF586E75",
            ResultsBoxTextColor = "#FFE6E6E6",
            ResultsBoxBackgroundColor = "#FF002B36",
            WindowBorderColor = "#FF586E75",
            WindowBackgroundColor = "#FF002B36",
            HighlightColor = "#FFB58900",
            NotificationBackgroundColor = "#FF073642",
            NotificationBorderColor = "#FF586E75",
            NotificationTextColor = "#FFE6E6E6"
        };

        public static readonly ColorPreset NordTheme = new ColorPreset
        {
            Name = "Nord",
            SearchBoxBorderColor = "#FF5E81AC",
            SearchBoxTextColor = "#FFECEFF4",
            SearchBoxBackgroundColor = "#FF4C566A",
            ResultsBoxBorderColor = "#FF5E81AC",
            ResultsBoxTextColor = "#FFECEFF4",
            ResultsBoxBackgroundColor = "#FF3B4252",
            WindowBorderColor = "#FF5E81AC",
            WindowBackgroundColor = "#FF2E3440",
            HighlightColor = "#FF88C0D0",
            NotificationBackgroundColor = "#FF4C566A",
            NotificationBorderColor = "#FF5E81AC",
            NotificationTextColor = "#FFECEFF4"
        };

        public static readonly ColorPreset LightTheme = new ColorPreset
        {
            Name = "Light",
            SearchBoxBorderColor = "#FFCCCCCC",
            SearchBoxTextColor = "#FF2D2D2D",
            SearchBoxBackgroundColor = "#FFF5F5F5",
            ResultsBoxBorderColor = "#FFCCCCCC",
            ResultsBoxTextColor = "#FF2D2D2D",
            ResultsBoxBackgroundColor = "#FFFFFFFF",
            WindowBorderColor = "#FFCCCCCC",
            WindowBackgroundColor = "#FFF8F8F8",
            HighlightColor = "#FF007ACC",
            NotificationBackgroundColor = "#FFF5F5F5",
            NotificationBorderColor = "#FFCCCCCC",
            NotificationTextColor = "#FF2D2D2D"
        };

        public static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    return settings ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            return new Settings();
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public string GetHotkeyDisplayString()
        {
            var modifiers = new List<string>();
            
            if ((HotkeyModifiers & ModifierKeys.Control) != 0)
                modifiers.Add("Ctrl");
            if ((HotkeyModifiers & ModifierKeys.Shift) != 0)
                modifiers.Add("Shift");
            if ((HotkeyModifiers & ModifierKeys.Alt) != 0)
                modifiers.Add("Alt");
            if ((HotkeyModifiers & ModifierKeys.Windows) != 0)
                modifiers.Add("Win");

            // Convert key to user-friendly display name
            string keyName = HotkeyKey switch
            {
                Key.Space => "Space",
                Key.Enter => "Enter",
                Key.Tab => "Tab",
                Key.Escape => "Escape",
                Key.Back => "Backspace",
                Key.Delete => "Delete",
                Key.Insert => "Insert",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "Page Up",
                Key.PageDown => "Page Down",
                Key.Up => "↑",
                Key.Down => "↓",
                Key.Left => "←",
                Key.Right => "→",
                _ => HotkeyKey.ToString()
            };
            
            modifiers.Add(keyName);
            
            return string.Join("+", modifiers);
        }
    }
} 