//using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using LapseOTron;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows;
using Utf8Json;
using Utf8Json.Resolvers;
using System.Reflection;
using System.Linq;
using LapseOTron.Settings;

namespace LapseOTron
{
    public enum SettingLoadState
    {
        Ok,
        NotFound,
        Error
    }

    /// <summary>
    /// Handles settings and runtime settings.
    /// - Runtime settings are lightweight quick settings that change often - e.g. current song position + playlist + window position.
    /// - User settings are more heavyweight configurations for the bulk of the application
    /// - Visualisation components have separately saved settings to make it easier to load individually (e.g. pre-defined variants)
    /// </summary>
    public static partial class SettingsWrangler
    {
        public static GeneralSettings GeneralSettings { get; set; }

        public static GeneralSettings DefaultSettings { get; } = new GeneralSettings();

        public static string SettingsFolder { get; set; }

        public static T LoadSettings<T>(ref SettingLoadState status, string subFolder = "") where T : new()
        {
            T results;
            IEnumerable<Attributes.ExternalFilename> attributes = typeof(T).GetCustomAttributes<Attributes.ExternalFilename>();
            string settingsFilename = attributes.First().Filename;
            string filename = Path.Combine(SettingsFolder, subFolder, settingsFilename);

            try
            {
                using TextReader reader = new StreamReader(filename);
                string json = reader.ReadToEnd();

                results = Utf8Json.JsonSerializer.Deserialize<T>(json);

                ((ISettings)results).SettingsFilename = settingsFilename;

                status = SettingLoadState.Ok;
            }
            catch (FileNotFoundException ex)
            {
                results = new T();
                Debugging.AddA($"✖ File not found loading '{settingsFilename}' settings, '{filename}'");
                Debugging.AddA($"   ✕ {ex.Message}");
                Debug.Print(ex.Message);
                status = SettingLoadState.NotFound;
            }
            catch (Exception ex)
            {
                results = new T();
                Debugging.AddA($"✖ Error loading '{settingsFilename}' settings, '{filename}'");
                Debugging.AddA($"   ✕ {ex.Message}");
                Debug.Print(ex.Message);
                status = SettingLoadState.Error;
            }

            return results;
        }

        /// <summary>
        /// Save settings will automatically save settings to a file only if they have changed, or a subfolder has been specified (to e.g. force saving of presets)
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="subFolder"></param>
        /// <returns>True if saved OK, or if no save was required. False if something went wrong</returns>
        public static bool SaveSettings(object settings, string subFolder = "")
        {
            bool debugEnabled = Globals.DebuggingEnabled;

            if (debugEnabled)
                Debugging.AddA($"Saving settings '{((ISettings)settings).SettingsFilename}'...");

            if (((ISettings)settings).SettingsChanged || subFolder.Length > 0)
            {
                try
                {
                    string filename = Path.Combine(SettingsWrangler.SettingsFolder, subFolder, ((ISettings)settings).SettingsFilename);
                    using (StreamWriter writer = new StreamWriter(filename))
                    {
                        writer.Write(Utf8Json.JsonSerializer.PrettyPrint(
                            Utf8Json.JsonSerializer.Serialize(settings, StandardResolver.ExcludeNull)
                        ));
                    }

                    ((ISettings)settings).SettingsChanged = false;

                    if (debugEnabled)
                        Debugging.AddA($"   ✔ Saved!");
                }
                catch (Exception ex)
                {
                    if (debugEnabled)
                    {
                        Debugging.AddA("   ✖ Error saving 'settings.SettingsFilename' settings...");
                        Debugging.AddA($"      ✕ {ex.Message}");
                    }

                    return false;
                }
            }

            return true;
        }

        public static void SaveAllSettings()
        {
            bool debuggingEnabled = Globals.DebuggingEnabled;

            if (debuggingEnabled)
                Debugging.AddA("Saving settings...");

            SaveSettings(GeneralSettings);

            if (debuggingEnabled)
                Debugging.AddA("...save complete");
        }

        private static void WriteSettingStateMessage(SettingLoadState state)
        {
            switch (state)
            {
                case SettingLoadState.Ok:
                    Debugging.AddA("   ✔ loaded OK");
                    break;
                case SettingLoadState.NotFound:
                    Debugging.AddA("   ⓘ not found - default settings will be used");
                    break;
                case SettingLoadState.Error:
                    Debugging.AddA("   ✖ error - default settings will be used");
                    break;
                default:
                    Debugging.AddA("   ✖ unknown error processing settings - default will be used");
                    break;
            }

        }
    }
}

namespace LapseOTron.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExternalFilename : Attribute
    {
        public string Filename { get; set; }

        public ExternalFilename(string filename)
        {
            Filename = filename;
        }
    }
}