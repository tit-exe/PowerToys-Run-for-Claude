using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    /// <summary>
    /// Guards the translations. A language that is missing a key falls back silently, so
    /// a half translated one would ship looking finished.
    /// </summary>
    [TestClass]
    public class LocalizationTests
    {
        private const string NeutralCulture = "en";

        private static Dictionary<string, Dictionary<string, string>> _catalog;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _ = context;

            var path = Repository.InProject("Strings.json");

            Assert.IsTrue(File.Exists(path), $"Strings.json not found at {path}.");

            _catalog = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
                File.ReadAllText(path));
        }

        [TestMethod]
        public void The_catalog_holds_english_and_the_translations()
        {
            Assert.IsTrue(_catalog.ContainsKey(NeutralCulture), "English is missing.");
            Assert.IsTrue(_catalog.Count >= 16, $"Only {_catalog.Count} languages were found.");
        }

        [TestMethod]
        public void English_declares_exactly_the_strings_the_code_reads()
        {
            // Neither side may drift: a key only in the code resolves to its own name in
            // the interface, and a key only in the file is dead weight nobody notices.
            CollectionAssert.AreEqual(
                ReadableStrings().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToList(),
                _catalog[NeutralCulture].Keys.OrderBy(name => name, StringComparer.Ordinal).ToList());
        }

        [TestMethod]
        public void Every_string_the_code_reads_resolves_to_real_text()
        {
            using var culture = new CultureScope(NeutralCulture);

            foreach (var property in ReadableStrings())
            {
                var value = (string)property.GetValue(null);

                Assert.IsFalse(string.IsNullOrWhiteSpace(value), property.Name);
                Assert.AreNotEqual(
                    property.Name,
                    value,
                    $"{property.Name} is missing from Strings.json and fell back to its own key.");
            }
        }

        [TestMethod]
        public void No_english_string_is_empty()
        {
            foreach (var pair in _catalog[NeutralCulture])
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(pair.Value), pair.Key);
            }
        }

        [TestMethod]
        public void Every_translation_covers_the_english_keys()
        {
            var expected = _catalog[NeutralCulture].Keys
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();

            foreach (var language in Translations())
            {
                CollectionAssert.AreEqual(
                    expected,
                    language.Value.Keys.OrderBy(key => key, StringComparer.Ordinal).ToList(),
                    $"{language.Key} does not translate exactly the English keys.");
            }
        }

        [TestMethod]
        public void No_translation_is_empty()
        {
            foreach (var language in Translations())
            {
                foreach (var pair in language.Value)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(pair.Value), $"{language.Key}/{pair.Key}");
                }
            }
        }

        [TestMethod]
        public void Every_translation_keeps_the_placeholders()
        {
            var english = _catalog[NeutralCulture];

            foreach (var language in Translations())
            {
                foreach (var pair in language.Value)
                {
                    CollectionAssert.AreEquivalent(
                        Placeholders(english[pair.Key]),
                        Placeholders(pair.Value),
                        $"{language.Key}/{pair.Key} does not use the same placeholders as English.");
                }
            }
        }

        [TestMethod]
        public void Every_translation_keeps_the_product_and_key_names()
        {
            // Translating either would point the user at something that does not exist.
            var english = _catalog[NeutralCulture];

            foreach (var language in Translations())
            {
                foreach (var pair in language.Value)
                {
                    foreach (var literal in new[] { "Claude", "Ctrl+" })
                    {
                        if (english[pair.Key].Contains(literal, StringComparison.Ordinal))
                        {
                            Assert.IsTrue(
                                pair.Value.Contains(literal, StringComparison.Ordinal),
                                $"{language.Key}/{pair.Key} lost \"{literal}\".");
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void Every_language_is_a_culture_windows_knows()
        {
            foreach (var name in _catalog.Keys)
            {
                Assert.AreEqual(
                    name,
                    CultureInfo.GetCultureInfo(name).Name,
                    "The culture code is not in its canonical form.");
            }
        }

        [TestMethod]
        public void Every_language_reaches_a_result()
        {
            using var culture = new CultureScope(NeutralCulture);
            using var plugin = new Main(new FakeLauncher());

            var english = Subtitle(plugin);

            foreach (var language in Translations())
            {
                CultureScope.Switch(language.Key);

                var subtitle = Subtitle(plugin);

                Assert.IsFalse(string.IsNullOrWhiteSpace(subtitle), language.Key);
                Assert.AreNotEqual(english, subtitle, $"{language.Key} fell back to English at run time.");
            }
        }

        [TestMethod]
        public void An_unknown_language_falls_back_to_english()
        {
            using var culture = new CultureScope(NeutralCulture);
            using var plugin = new Main(new FakeLauncher());

            var english = Subtitle(plugin);

            foreach (var name in new[] { "is", "mt", "cy", "af" })
            {
                CultureScope.Switch(name);

                Assert.AreEqual(english, Subtitle(plugin), name);
            }
        }

        [TestMethod]
        public void A_regional_variant_uses_its_closest_language()
        {
            using var culture = new CultureScope(NeutralCulture);
            using var plugin = new Main(new FakeLauncher());

            foreach (var (regional, closest) in new[]
            {
                ("fr-CA", "fr"),
                ("de-AT", "de"),
                ("es-MX", "es"),
                ("pt-PT", "pt"),
                ("zh-CN", "zh-Hans"),
                ("zh-TW", "zh-Hant"),
                ("ar-SA", "ar"),
            })
            {
                CultureScope.Switch(closest);
                var expected = Subtitle(plugin);

                CultureScope.Switch(regional);

                Assert.AreEqual(expected, Subtitle(plugin), $"{regional} did not resolve to {closest}.");
            }
        }

        [TestMethod]
        public void Switching_language_back_and_forth_keeps_working()
        {
            // The lookup caches the language it resolved last, so it has to notice a
            // change in both directions.
            using var culture = new CultureScope(NeutralCulture);
            using var plugin = new Main(new FakeLauncher());

            var seen = new List<string>();

            foreach (var name in new[] { "en", "fr", "en", "ja", "fr", "en" })
            {
                CultureScope.Switch(name);
                seen.Add(Subtitle(plugin));
            }

            Assert.AreEqual(seen[0], seen[2]);
            Assert.AreEqual(seen[0], seen[5]);
            Assert.AreEqual(seen[1], seen[4]);
            Assert.AreEqual(3, seen.Distinct(StringComparer.Ordinal).Count());
        }

        private static PropertyInfo[] ReadableStrings() =>
            typeof(Strings).GetProperties(BindingFlags.NonPublic | BindingFlags.Static);

        private static IEnumerable<KeyValuePair<string, Dictionary<string, string>>> Translations() =>
            _catalog.Where(language => language.Key != NeutralCulture)
                .OrderBy(language => language.Key, StringComparer.Ordinal);

        private static string Subtitle(Main plugin) =>
            plugin.Query(new Query("claude", "claude")).Single().SubTitle;

        private static List<string> Placeholders(string value) =>
            Regex.Matches(value, @"\{\d+\}")
                .Select(match => match.Value)
                .OrderBy(text => text, StringComparer.Ordinal)
                .ToList();
    }
}
