using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.PowerToys.Settings.UI.Library;

namespace Community.PowerToys.Run.Plugin.Claude
{
    /// <summary>
    /// The option shown on the plugin's page in PowerToys Settings, and the value
    /// currently in effect.
    /// </summary>
    public sealed class ClaudeSettings
    {
        /// <summary>
        /// The option key PowerToys writes to disk. Renaming it resets the option for
        /// everyone who already has the plugin, so it is part of its contract.
        /// </summary>
        public const string OpenInKey = "OpenIn";

        /// <summary>Gets where a result opens Claude.</summary>
        public ClaudeOpenIn OpenIn { get; private set; } = ClaudeOpenIn.DesktopApp;

        /// <summary>
        /// Gets the option definition PowerToys Settings renders.
        /// </summary>
        public static IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>
        {
            new PluginAdditionalOption
            {
                Key = OpenInKey,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                DisplayLabel = Strings.OptionOpenInLabel,
                DisplayDescription = Strings.OptionOpenInDescription,
                ComboBoxValue = (int)ClaudeOpenIn.DesktopApp,
                ComboBoxItems = new List<KeyValuePair<string, string>>
                {
                    Choice(Strings.OptionOpenInApp, ClaudeOpenIn.DesktopApp),
                    Choice(Strings.OptionOpenInBrowser, ClaudeOpenIn.DefaultBrowser),
                    Choice(Strings.OptionOpenInBoth, ClaudeOpenIn.Both),
                },
            },
        };

        /// <summary>
        /// Builds one dropdown entry. The stored value comes from the enumeration rather
        /// than a literal, so the two can never drift apart.
        /// </summary>
        private static KeyValuePair<string, string> Choice(string label, ClaudeOpenIn destination) =>
            new KeyValuePair<string, string>(label, ((int)destination).ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// Applies the value PowerToys has stored, falling back to the default so that a
        /// settings file written by another version still loads.
        /// </summary>
        /// <param name="settings">The stored settings; may be null.</param>
        public void Update(PowerLauncherPluginSettings settings)
        {
            var stored = settings?.AdditionalOptions
                ?.FirstOrDefault(option => string.Equals(option.Key, OpenInKey, StringComparison.Ordinal))
                ?.ComboBoxValue;

            OpenIn = stored.HasValue && Enum.IsDefined(typeof(ClaudeOpenIn), stored.Value)
                ? (ClaudeOpenIn)stored.Value
                : ClaudeOpenIn.DesktopApp;
        }
    }
}
