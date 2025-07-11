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