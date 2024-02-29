using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Aria2Fast.Utils
{
    /*
     var configManager = new ConfigFileManager("path/to/config/file.conf");

        // 读取值
        string diskCache = configManager.GetValue("disk-cache");
        Console.WriteLine($"Disk-cache: {diskCache}");

        // 设置新值
        configManager.SetValue("disk-cache", "128M");

        // 再次读取设置后的新值
        diskCache = configManager.GetValue("disk-cache");
        Console.WriteLine($"Updated Disk-cache: {diskCache}");

     */


    public class ConfigFileManager
    {
        private string _filePath;
        private List<string> _lines;
        private Dictionary<string, string> _keyValuePairs;

        public ConfigFileManager(string filePath)
        {
            _filePath = filePath;
            _lines = File.ReadAllLines(filePath, Encoding.UTF8).ToList();
            _keyValuePairs = new Dictionary<string, string>();

            foreach (var line in _lines)
            {
                if (line.Trim().StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    continue; // Skip comments and blank lines
                }

                var splitLine = line.Split(new[] { '=' }, 2); // Split into 2 parts only

                if (splitLine.Length == 2)
                {
                    _keyValuePairs[splitLine[0].Trim()] = splitLine[1].Trim();
                }
            }
        }

        public string GetValue(string key)
        {
            return _keyValuePairs.TryGetValue(key, out var value) ? value : null;
        }

        public void SetValue(string key, string value)
        {
            if (!_keyValuePairs.ContainsKey(key))
            {
                throw new ArgumentException($"The key '{key}' does not exist in the configuration file.");
            }

            _keyValuePairs[key] = value; // Update our dictionary

            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].StartsWith(key))
                {
                    _lines[i] = $"{key}={value}"; // Update the specific line
                    break;
                }
            }

            Save(); // Save the changes immediately
        }

        private void Save()
        {
            File.WriteAllLines(_filePath, _lines, Encoding.UTF8);
        }
    }
}
