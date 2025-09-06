// Services/SettingsService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace GSADUsRevitAddin.Services
{
    public sealed class SettingsService
    {
        public static SettingsService Instance { get; } = new SettingsService();

        public Settings Current { get; private set; } = Settings.CreateDefault();

        public string AppDataDir { get; }
        public string LogsDir { get; }
        public string SettingsFilePath { get; }

        private SettingsService()
        {
            AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GSADUsRevitAddin");
            LogsDir = Path.Combine(AppDataDir, "logs");
            Directory.CreateDirectory(AppDataDir);
            Directory.CreateDirectory(LogsDir);
            SettingsFilePath = Path.Combine(AppDataDir, "settings.json");
            Load();
        }

        public void Load()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
                    Current = JsonConvert.DeserializeObject<Settings>(json) ?? Settings.CreateDefault();
                }
                catch
                {
                    Current = Settings.CreateDefault();
                }
            }
            else
            {
                Current = Settings.CreateDefault();
                Save();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(AppDataDir);
            var json = JsonConvert.SerializeObject(Current, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json, Encoding.UTF8);
        }

        public string GetExportRootPath()
        {
            var path = Current.ExportRootPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GSADUsRevitAddin", "Exports");
            }
            Directory.CreateDirectory(path);
            return path;
        }

        public void SetExportRootPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            Current.ExportRootPath = path;
            Save();
        }
    }

    public sealed class Settings
    {
        public string ExportRootPath { get; set; }
        public List<string> LastUsedCategories { get; set; } = new List<string>();
        public double? ProximityFeet { get; set; }
        public Dictionary<string, string> Custom { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static Settings CreateDefault() => new Settings();
    }
}
