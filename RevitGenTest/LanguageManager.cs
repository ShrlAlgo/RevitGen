using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace RevitGenTest
{
    /// <summary>
    /// Manages language/locale preferences for the addin.
    /// Supports zh-CN (Chinese), en (English), and ja (Japanese).
    /// </summary>
    public static class LanguageManager
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RevitGenTest");

        private static readonly string SettingsFilePath = Path.Combine(
            SettingsDirectory,
            "language.txt");

        /// <summary>
        /// The language codes supported by this addin.
        /// </summary>
        public static readonly string[] SupportedLanguages = { "zh-CN", "en", "ja" };

        /// <summary>
        /// Gets the current UI language code.
        /// </summary>
        public static string CurrentLanguage => Thread.CurrentThread.CurrentUICulture.Name;

        /// <summary>
        /// Loads the saved language preference and applies it to the current thread.
        /// Falls back to the default culture if no preference is saved or if it cannot be loaded.
        /// </summary>
        public static void LoadLanguagePreference()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var language = File.ReadAllText(SettingsFilePath).Trim();
                    ApplyLanguage(language);
                }
            }
            catch (IOException)
            {
                // Fall back to system default if loading fails
            }
            catch (UnauthorizedAccessException)
            {
                // Fall back to system default if access is denied
            }
        }

        /// <summary>
        /// Saves the language preference to disk so it persists across sessions.
        /// </summary>
        /// <param name="languageCode">The BCP 47 language code to save (e.g. "zh-CN", "en", "ja").</param>
        public static void SaveLanguagePreference(string languageCode)
        {
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                    Directory.CreateDirectory(SettingsDirectory);

                File.WriteAllText(SettingsFilePath, languageCode);
            }
            catch (IOException)
            {
                // Ignore save failures silently
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore save failures silently
            }
        }

        /// <summary>
        /// Switches the UI language to the specified culture code and saves the preference.
        /// </summary>
        /// <param name="languageCode">The BCP 47 language code to apply (e.g. "zh-CN", "en", "ja").</param>
        public static void SwitchLanguage(string languageCode)
        {
            ApplyLanguage(languageCode);
            SaveLanguagePreference(languageCode);
        }

        private static void ApplyLanguage(string languageCode)
        {
            try
            {
                var culture = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentUICulture = culture;
                Properties.Resources.Culture = culture;
            }
            catch (CultureNotFoundException)
            {
                // Fall back to default culture if the specified code is not recognized
            }
        }
    }
}
