using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace Aria2Fast.Service
{
    public static class StartupLaunchManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupEntryName = "Aria2Fast";
        private const string ProcessFileName = "Aria2Fast.exe";

        public static void Sync(bool enabled)
        {
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                    ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

                if (runKey is null)
                {
                    EasyLogManager.Logger.Error("Cannot access startup registry key.");
                    return;
                }

                RemoveAllAria2FastEntries(runKey);

                if (!enabled)
                {
                    EasyLogManager.Logger.Info("Startup launch disabled.");
                    return;
                }

                var currentExePath = GetCurrentExecutablePath();
                if (string.IsNullOrWhiteSpace(currentExePath))
                {
                    EasyLogManager.Logger.Error("Cannot resolve executable path for startup launch.");
                    return;
                }

                runKey.SetValue(StartupEntryName, $"\"{currentExePath}\"", RegistryValueKind.String);
                EasyLogManager.Logger.Info($"Startup launch set: {currentExePath}");
            }
            catch (Exception ex)
            {
                EasyLogManager.Logger.Error(ex);
            }
        }

        private static void RemoveAllAria2FastEntries(RegistryKey runKey)
        {
            foreach (var valueName in runKey.GetValueNames())
            {
                try
                {
                    var valueData = runKey.GetValue(valueName)?.ToString();
                    if (!IsAria2FastRunEntry(valueData))
                    {
                        continue;
                    }

                    runKey.DeleteValue(valueName, false);
                    EasyLogManager.Logger.Info($"Removed startup entry: {valueName}");
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error($"Failed to remove startup entry: {valueName} {ex}");
                }
            }
        }

        internal static bool IsAria2FastRunEntry(string? valueData)
        {
            var executablePath = ExtractExecutablePath(valueData);
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return false;
            }

            var fileName = Path.GetFileName(executablePath);
            return string.Equals(fileName, ProcessFileName, StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractExecutablePath(string? command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return string.Empty;
            }

            var text = command.Trim();
            if (text.StartsWith('"'))
            {
                var endQuote = text.IndexOf('"', 1);
                if (endQuote > 1)
                {
                    return text.Substring(1, endQuote - 1).Trim();
                }

                return text.Trim('"').Trim();
            }

            const string exeExt = ".exe";
            var exeIndex = text.IndexOf(exeExt, StringComparison.OrdinalIgnoreCase);
            if (exeIndex >= 0)
            {
                return text.Substring(0, exeIndex + exeExt.Length).Trim();
            }

            var firstSpace = text.IndexOf(' ');
            if (firstSpace > 0)
            {
                return text.Substring(0, firstSpace).Trim();
            }

            return text;
        }

        private static string GetCurrentExecutablePath()
        {
            if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
            {
                return Environment.ProcessPath!;
            }

            try
            {
                return Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
