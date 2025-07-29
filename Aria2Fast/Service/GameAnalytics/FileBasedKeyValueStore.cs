using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Services.GameAnalytics
{
    using Aria2Fast.Utils;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    // --- Enums corresponding to Objective-C enums ---
    public enum ProgressionStatus
    {
        Start,
        Fail,
        Complete
    }

    public enum ResourceFlowType
    {
        Source,
        Sink
    }

    public enum ErrorType
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// A placeholder for an application utility class.
    /// You should implement these methods based on your application's platform (e.g., .NET, Unity).
    /// </summary>
    public static class AppUtils
    {
        public static string DeviceSystemVersion() => Environment.OSVersion.VersionString;
        public static string DeviceModelName() => "unknown"; // Implement actual device model retrieval
        public static string BundleFullVersion() => ActionVersion.Version + "." + ActionVersion.Build; // Implement actual app version retrieval
    }

    /// <summary>
    /// Simple file-based key-value store to replace MMKV's functionality for this example.
    /// It serializes a dictionary to a JSON file.
    /// NOTE: This is not optimized for performance like MMKV.
    /// </summary>
    public class FileBasedKeyValueStore
    {
        private readonly string _filePath;
        private readonly object _lock = new object();
        private Dictionary<string, object> _data;

        public FileBasedKeyValueStore(string storeName)
        {
            // Use a local app data folder to store the file
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dirPath = Path.Combine(appDataPath, "GameAnalyticsClientCS");
            Directory.CreateDirectory(dirPath);
            _filePath = Path.Combine(dirPath, $"{storeName}.json");
            Load();
        }

        private void Load()
        {
            lock (_lock)
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                }
                else
                {
                    _data = new Dictionary<string, object>();
                }
            }
        }

        private void Save()
        {
            lock (_lock)
            {
                var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
        }

        public void SetValue<T>(string key, T value)
        {
            lock (_lock)
            {
                _data[key] = value;
            }
            Save();
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            lock (_lock)
            {
                if (_data.TryGetValue(key, out var value) && value is T typedValue)
                {
                    return typedValue;
                }

                // Handle cases where the number is stored as long (from JSON deserialization)
                if (value != null)
                {
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch { /* Conversion failed, return default */ }
                }
            }
            return defaultValue;
        }

        public JObject GetObject<JObject>(string key) where JObject : class
        {
            lock (_lock)
            {
                if (_data.TryGetValue(key, out var value) && value is Newtonsoft.Json.Linq.JObject jObject)
                {
                    return jObject.ToObject<JObject>();
                }
            }
            return null;
        }

        public void RemoveValue(string key)
        {
            lock (_lock)
            {
                if (_data.Remove(key))
                {
                    Save();
                }
            }
        }
    }
}
