using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Aria2Fast.Service;
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

            var normalizedTheme = NormalizeTheme(theme);
            var appTheme = normalizedTheme == CatppuccinTheme.Latte || normalizedTheme == CatppuccinTheme.Default
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark;

            var updateAccent = normalizedTheme == CatppuccinTheme.Default;
            ApplicationThemeManager.Apply(appTheme, WindowBackdropType.None, updateAccent);

            if (normalizedTheme != CatppuccinTheme.Default)
            {
                var accent = GetAccent(normalizedTheme);
                ApplicationAccentColorManager.Apply(accent, appTheme, false);
            }

            ReplaceThemeDictionary(normalizedTheme);
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

            var themeSource = new Uri($"{ThemeUriPrefix}{theme}{ThemeUriSuffix}", UriKind.Absolute);
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
                CatppuccinTheme.Latte => Color.FromRgb(0x1E, 0x66, 0xF5),
                CatppuccinTheme.Frappe => Color.FromRgb(0x8C, 0xAA, 0xEE),
                CatppuccinTheme.Macchiato => Color.FromRgb(0x8A, 0xAD, 0xF4),
                CatppuccinTheme.Mocha => Color.FromRgb(0x89, 0xB4, 0xFA),
                _ => Colors.Blue
            };
        }

        private static CatppuccinTheme NormalizeTheme(CatppuccinTheme theme)
        {
            return theme switch
            {
                CatppuccinTheme.Default => CatppuccinTheme.Default,
                CatppuccinTheme.Latte => CatppuccinTheme.Latte,
                CatppuccinTheme.Frappe => CatppuccinTheme.Frappe,
                CatppuccinTheme.Macchiato => CatppuccinTheme.Macchiato,
                CatppuccinTheme.Mocha => CatppuccinTheme.Mocha,
                _ => CatppuccinTheme.Default
            };
        }
    }
}
