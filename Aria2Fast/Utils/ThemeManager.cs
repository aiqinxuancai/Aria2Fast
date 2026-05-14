using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Aria2Fast.Service;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Aria2Fast.Utils
{
    public static class ThemeManager
    {
        private const string ThemeUriPrefix = "pack://application:,,,/Theme/Catppuccin/";
        private const string ThemeUriSuffix = ".xaml";

        public static void ApplyTheme(CatppuccinTheme theme, FrameworkElement? target = null)
        {
            if (Application.Current is null)
            {
                return;
            }

            var normalizedTheme = NormalizeConfiguredTheme(theme);
            var effectiveTheme = ResolveEffectiveTheme(normalizedTheme);
            var appTheme = effectiveTheme == CatppuccinTheme.Dark
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;

            var updateAccent = effectiveTheme == CatppuccinTheme.Light;
            ApplicationThemeManager.Apply(appTheme, WindowBackdropType.None, updateAccent);

            if (effectiveTheme == CatppuccinTheme.Dark)
            {
                var accent = GetAccent(effectiveTheme);
                ApplicationAccentColorManager.Apply(accent, appTheme, false);
            }

            ReplaceThemeDictionary(effectiveTheme);
        }

        public static bool IsAutoTheme(CatppuccinTheme theme)
        {
            return NormalizeConfiguredTheme(theme) == CatppuccinTheme.Auto;
        }

        private static void ReplaceThemeDictionary(CatppuccinTheme theme)
        {
            if (Application.Current is null)
            {
                return;
            }

            RemoveThemeDictionaries(Application.Current.Resources.MergedDictionaries);
            foreach (Window window in Application.Current.Windows)
            {
                RemoveThemeDictionaries(window.Resources.MergedDictionaries);
            }

            var themeName = GetThemeResourceName(theme);
            var themeSource = new Uri($"{ThemeUriPrefix}{themeName}{ThemeUriSuffix}", UriKind.Absolute);
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeSource });
        }

        private static void RemoveThemeDictionaries(IList<ResourceDictionary> dictionaries)
        {
            for (var i = dictionaries.Count - 1; i >= 0; i--)
            {
                var source = dictionaries[i].Source;
                if (source == null)
                {
                    continue;
                }

                if (source.OriginalString.IndexOf("/Theme/Catppuccin/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    dictionaries.RemoveAt(i);
                }
            }
        }

        private static Color GetAccent(CatppuccinTheme theme)
        {
            return theme switch
            {
                CatppuccinTheme.Dark => Color.FromRgb(0x8C, 0xAA, 0xEE),
                _ => Colors.Blue
            };
        }

        public static CatppuccinTheme NormalizeConfiguredTheme(CatppuccinTheme theme)
        {
            return theme switch
            {
                CatppuccinTheme.Auto => CatppuccinTheme.Auto,
                CatppuccinTheme.Light => CatppuccinTheme.Light,
                CatppuccinTheme.Dark => CatppuccinTheme.Dark,
                CatppuccinTheme.Default => CatppuccinTheme.Light,
                CatppuccinTheme.Latte => CatppuccinTheme.Light,
                CatppuccinTheme.Frappe => CatppuccinTheme.Dark,
                CatppuccinTheme.Macchiato => CatppuccinTheme.Dark,
                CatppuccinTheme.Mocha => CatppuccinTheme.Dark,
                _ => CatppuccinTheme.Auto
            };
        }

        private static CatppuccinTheme ResolveEffectiveTheme(CatppuccinTheme theme)
        {
            return theme == CatppuccinTheme.Auto
                ? GetSystemPreferredTheme()
                : theme;
        }

        private static string GetThemeResourceName(CatppuccinTheme theme)
        {
            return theme switch
            {
                CatppuccinTheme.Dark => nameof(CatppuccinTheme.Frappe),
                _ => nameof(CatppuccinTheme.Default)
            };
        }

        private static CatppuccinTheme GetSystemPreferredTheme()
        {
            const string personalizeKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string appsUseLightTheme = "AppsUseLightTheme";

            try
            {
                var value = Registry.GetValue(personalizeKey, appsUseLightTheme, 1);
                if (value is int intValue && intValue == 0)
                {
                    return CatppuccinTheme.Dark;
                }
            }
            catch
            {
                // ignore and fallback to light theme
            }

            return CatppuccinTheme.Light;
        }
    }
}
