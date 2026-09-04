using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PowerToys.Settings.UI.Library;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    /// <summary>
    /// Builds settings the way PowerToys does: start from the declared options, change
    /// the ones under test, then hand the whole set to the plugin.
    /// </summary>
    internal static class SettingsFactory
    {
        public static ClaudeSettings Create(Action<List<PluginAdditionalOption>> configure = null)
        {
            var options = ClaudeSettings.AdditionalOptions.ToList();
            configure?.Invoke(options);

            var settings = new ClaudeSettings();
            settings.Update(new PowerLauncherPluginSettings { AdditionalOptions = options });

            return settings;
        }

        public static PowerLauncherPluginSettings Options(Action<List<PluginAdditionalOption>> configure = null)
        {
            var options = ClaudeSettings.AdditionalOptions.ToList();
            configure?.Invoke(options);

            return new PowerLauncherPluginSettings { AdditionalOptions = options };
        }

        public static void SetChoice(this List<PluginAdditionalOption> options, string key, int value) =>
            Get(options, key).ComboBoxValue = value;

        private static PluginAdditionalOption Get(List<PluginAdditionalOption> options, string key)
        {
            var option = options.FirstOrDefault(candidate => candidate.Key == key);

            if (option is null)
            {
                throw new InvalidOperationException($"No option is declared with the key '{key}'.");
            }

            return option;
        }
    }
}
