using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Community.PowerToys.Run.Plugin.Claude
{
    /// <summary>
    /// Every string the plugin shows, in every language it ships.
    /// </summary>
    /// <remarks>
    /// The translations live in <c>Strings.json</c>, embedded in this assembly, rather
    /// than in satellite assemblies. That keeps the plugin a single file: one thing to
    /// install, one thing to read, and no per language folder that could hide code.
    /// </remarks>
    internal static class Strings
    {
        private const string ResourceName = "Community.PowerToys.Run.Plugin.Claude.Strings.json";
        private const string NeutralCulture = "en";

        private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> Catalog =
            new Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>(Load);

        /// <summary>
        /// The language resolved for the last lookup. Reading a stale value only costs
        /// one extra resolution, and a reference assignment is atomic, so no lock is
        /// needed on the query path.
        /// </summary>
        private static ResolvedLanguage _resolved;

        internal static string PluginName => Get(nameof(PluginName));

        internal static string PluginDescription => Get(nameof(PluginDescription));

        internal static string TitleNewChat => Get(nameof(TitleNewChat));

        internal static string SubtitleInApp => Get(nameof(SubtitleInApp));

        internal static string SubtitleInBrowser => Get(nameof(SubtitleInBrowser));

        internal static string ContextOpenInApp => Get(nameof(ContextOpenInApp));

        internal static string ContextOpenInBrowser => Get(nameof(ContextOpenInBrowser));

        internal static string ContextCopyLink => Get(nameof(ContextCopyLink));

        internal static string ErrorAppLaunch => Get(nameof(ErrorAppLaunch));

        internal static string ErrorBrowserLaunch => Get(nameof(ErrorBrowserLaunch));

        internal static string ErrorClipboard => Get(nameof(ErrorClipboard));

        internal static string OptionOpenInLabel => Get(nameof(OptionOpenInLabel));

        internal static string OptionOpenInDescription => Get(nameof(OptionOpenInDescription));

        internal static string OptionOpenInApp => Get(nameof(OptionOpenInApp));

        internal static string OptionOpenInBrowser => Get(nameof(OptionOpenInBrowser));

        internal static string OptionOpenInBoth => Get(nameof(OptionOpenInBoth));

        internal static string WarningAmbiguousCommandTitle => Get(nameof(WarningAmbiguousCommandTitle));

        internal static string WarningAmbiguousCommandDescription => Get(nameof(WarningAmbiguousCommandDescription));

        private static string Get(string key)
        {
            var culture = CultureInfo.CurrentUICulture;
            var resolved = _resolved;

            if (resolved is null || !string.Equals(resolved.Culture, culture.Name, StringComparison.Ordinal))
            {
                resolved = new ResolvedLanguage(culture.Name, Resolve(culture));
                _resolved = resolved;
            }

            return resolved.Entries.TryGetValue(key, out var value) ? value : key;
        }

        /// <summary>
        /// Picks the closest language the plugin ships, walking up from the interface
        /// language: zh-CN gives zh-Hans, fr-CA gives fr, and anything unknown gives
        /// English.
        /// </summary>
        private static IReadOnlyDictionary<string, string> Resolve(CultureInfo culture)
        {
            var catalog = Catalog.Value;

            for (var candidate = culture; !string.IsNullOrEmpty(candidate.Name); candidate = candidate.Parent)
            {
                if (catalog.TryGetValue(candidate.Name, out var translated))
                {
                    return translated;
                }
            }

            return catalog.TryGetValue(NeutralCulture, out var neutral)
                ? neutral
                : new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Reads the embedded catalog, filling every language out with the English
        /// text so a lookup never has to fall back a second time.
        /// </summary>
        private static Dictionary<string, IReadOnlyDictionary<string, string>> Load()
        {
            var catalog = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            using var stream = typeof(Strings).Assembly.GetManifestResourceStream(ResourceName);

            if (stream is null)
            {
                return catalog;
            }

            using var document = JsonDocument.Parse(stream);

            var english = new Dictionary<string, string>(StringComparer.Ordinal);

            if (document.RootElement.TryGetProperty(NeutralCulture, out var neutral))
            {
                foreach (var entry in neutral.EnumerateObject())
                {
                    english[entry.Name] = entry.Value.GetString();
                }
            }

            foreach (var language in document.RootElement.EnumerateObject())
            {
                var strings = new Dictionary<string, string>(english, StringComparer.Ordinal);

                foreach (var entry in language.Value.EnumerateObject())
                {
                    strings[entry.Name] = entry.Value.GetString();
                }

                catalog[language.Name] = strings;
            }

            return catalog;
        }

        /// <summary>
        /// The strings of one language, together with the interface language they were
        /// resolved from.
        /// </summary>
        private sealed class ResolvedLanguage
        {
            internal ResolvedLanguage(string culture, IReadOnlyDictionary<string, string> entries)
            {
                Culture = culture;
                Entries = entries;
            }

            internal string Culture { get; }

            internal IReadOnlyDictionary<string, string> Entries { get; }
        }
    }
}
