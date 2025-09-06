using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace GSADUs.Core
{
    public class BatchExportSettings
    {
        public bool SaveBeforeExport { get; set; } = true;
        public string ExportRoot { get; set; } = "";
        public string LogFolder { get; set; } = "";
        public int LimitCount { get; set; } = 0;
        public int PurgePasses { get; set; } = 3;
        public bool SaveCompact { get; set; } = true;
        public bool SavePreview { get; set; } = false;
    }

    public static class SettingsManager
    {
        public static BatchExportSettings Load(string path)
        {
            if (!File.Exists(path))
                return new BatchExportSettings();

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonConvert.DeserializeObject<BatchExportSettings>(json) ?? new BatchExportSettings();
        }

        public static void Save(string path, BatchExportSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        public static bool IsValid(BatchExportSettings settings)
        {
            // Example validation: paths must not be null, limit must be non-negative
            return settings != null
                && settings.PurgePasses > 0
                && settings.LimitCount >= 0;
        }

        public static BatchExportSettings ResetToDefaults()
        {
            return new BatchExportSettings();
        }

        public static bool TryLoad(string path, out BatchExportSettings settings)
        {
            settings = null;
            try
            {
                settings = Load(path);
                return true;
            }
            catch (Exception)
            {
                settings = null;
                return false;
            }
        }

        public static string GetSettingsPath()
        {
            // Return the default path for the settings file
            return Path.Combine(Environment.CurrentDirectory, "BatchExportSettings.json");
        }

        public static void EnsureDirectoriesExist(BatchExportSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Ensure that the directories for ExportRoot and LogFolder exist
            if (!string.IsNullOrEmpty(settings.ExportRoot) && !Directory.Exists(settings.ExportRoot))
                Directory.CreateDirectory(settings.ExportRoot);

            if (!string.IsNullOrEmpty(settings.LogFolder) && !Directory.Exists(settings.LogFolder))
                Directory.CreateDirectory(settings.LogFolder);
        }

        public static void ValidatePaths(BatchExportSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            // Validate and resolve paths, throwing exceptions if they are invalid
            if (string.IsNullOrEmpty(settings.ExportRoot))
                throw new InvalidOperationException("ExportRoot must be set.");

            if (string.IsNullOrEmpty(settings.LogFolder))
                throw new InvalidOperationException("LogFolder must be set.");

            // Additional path validation logic can be added here
        }
    }
}