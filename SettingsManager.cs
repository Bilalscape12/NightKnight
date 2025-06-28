using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NightKnight
{
    public class SettingsManager
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NightKnight"
        );
        
        private static readonly string IntervalsFilePath = Path.Combine(SettingsDirectory, "intervals.json");
        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

        public static void SaveIntervals(ObservableCollection<FilterInterval> intervals)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(SettingsDirectory);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(intervals, options);
                File.WriteAllText(IntervalsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save intervals: {ex.Message}", "Save Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        public static List<FilterInterval> LoadIntervals()
        {
            try
            {
                if (!File.Exists(IntervalsFilePath))
                {
                    return [];
                }

                string json = File.ReadAllText(IntervalsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var intervals = JsonSerializer.Deserialize<List<FilterInterval>>(json, options);
                return intervals ?? [];
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load intervals: {ex.Message}", "Load Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return [];
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(SettingsDirectory);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save settings: {ex.Message}", "Save Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        public static AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return new AppSettings();
                }

                string json = File.ReadAllText(SettingsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load settings: {ex.Message}", "Load Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return new AppSettings();
            }
        }
    }

    public class AppSettings
    {
        [JsonPropertyName("scheduleEnabled")]
        public bool ScheduleEnabled { get; set; } = false;
        
        [JsonPropertyName("userLatitude")]
        public double UserLatitude { get; set; } = 40.7128; // Default to New York
        
        [JsonPropertyName("userLongitude")]
        public double UserLongitude { get; set; } = -74.0060;
        
        [JsonPropertyName("timeZoneOffset")]
        public int TimeZoneOffset { get; set; } = -5; // Default to EST
    }
} 