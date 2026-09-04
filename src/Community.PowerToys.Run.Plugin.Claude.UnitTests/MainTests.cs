using System;
using System.Globalization;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    [TestClass]
    public sealed class MainTests : IDisposable
    {
        private const string Keyword = "cl:";
        private const string AppLink = "claude://claude.ai/new?q=hello";
        private const string WebLink = "https://claude.ai/new?q=hello";

        private FakeLauncher _launcher;
        private Main _plugin;

        [TestInitialize]
        public void TestInitialize()
        {
            _launcher = new FakeLauncher();
            _plugin = new Main(_launcher);
        }

        /// <summary>
        /// Releases the plugin built for the current test. MSTest creates one instance
        /// per test method and disposes it when the method returns.
        /// </summary>
        public void Dispose() => _plugin.Dispose();

        [TestMethod]
        public void A_query_opens_the_desktop_app_by_default()
        {
            var results = _plugin.Query(Typed("what is the date today"));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("what is the date today", results[0].Title);

            Assert.IsTrue(results[0].Action(null));
            Assert.AreEqual("claude://claude.ai/new?q=what%20is%20the%20date%20today", _launcher.DeepLinks.Single());
        }

        [TestMethod]
        public void A_refused_launch_reports_failure_without_opening_anything_else()
        {
            _launcher.DeepLinkSucceeds = false;

            var result = _plugin.Query(Typed("hello")).Single();

            Assert.IsFalse(result.Action(null));
            Assert.AreEqual(AppLink, _launcher.DeepLinks.Single());
            Assert.AreEqual(0, _launcher.BrowserLinks.Count);
        }

        [TestMethod]
        public void Running_a_result_hands_the_link_to_the_launcher()
        {
            var result = _plugin.Query(Typed("hello")).Single();

            Assert.IsTrue(result.Action(null));
            Assert.AreEqual(AppLink, _launcher.DeepLinks.Single());
            Assert.AreEqual(0, _launcher.BrowserLinks.Count);
        }

        [TestMethod]
        public void The_browser_destination_never_tries_the_app()
        {
            _plugin.UpdateSettings(SettingsFactory.Options(options =>
                options.SetChoice(ClaudeSettings.OpenInKey, (int)ClaudeOpenIn.DefaultBrowser)));

            var result = _plugin.Query(Typed("hello")).Single();

            Assert.IsTrue(result.Action(null));
            Assert.AreEqual(0, _launcher.DeepLinks.Count);
            Assert.AreEqual(WebLink, _launcher.BrowserLinks.Single());
        }

        [TestMethod]
        public void Both_destinations_produce_two_results_the_app_first()
        {
            _plugin.UpdateSettings(SettingsFactory.Options(options =>
                options.SetChoice(ClaudeSettings.OpenInKey, (int)ClaudeOpenIn.Both)));

            var results = _plugin.Query(Typed("hello"));

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results[0].Score > results[1].Score);
            Assert.AreNotEqual(results[0].SubTitle, results[1].SubTitle);

            Assert.IsTrue(results[0].Action(null));
            Assert.IsTrue(results[1].Action(null));
            Assert.AreEqual(AppLink, _launcher.DeepLinks.Single());
            Assert.AreEqual(WebLink, _launcher.BrowserLinks.Single());
        }

        [TestMethod]
        public void Every_result_carries_a_title_a_subtitle_and_an_icon()
        {
            _plugin.UpdateSettings(SettingsFactory.Options(options =>
                options.SetChoice(ClaudeSettings.OpenInKey, (int)ClaudeOpenIn.Both)));

            foreach (var result in _plugin.Query(Typed("hello")))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.Title));
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.SubTitle));
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.QueryTextDisplay));
                Assert.IsNotNull(result.ContextData);
            }
        }

        [TestMethod]
        public void Short_global_queries_are_left_alone()
        {
            Assert.AreEqual(0, _plugin.Query(Global("hi")).Count);
            Assert.AreEqual(1, _plugin.Query(Global("hi!")).Count);
        }

        [TestMethod]
        public void The_global_threshold_does_not_apply_behind_the_activation_command()
        {
            Assert.AreEqual(1, _plugin.Query(Typed("a")).Count);
        }

        [TestMethod]
        public void Global_results_rank_below_keyword_results()
        {
            var global = _plugin.Query(Global("what is the date today")).Single();
            var keyword = _plugin.Query(Typed("what is the date today")).Single();

            Assert.IsTrue(global.Score < keyword.Score);
        }

        [TestMethod]
        public void An_empty_global_query_produces_nothing()
        {
            Assert.AreEqual(0, _plugin.Query(new Query(string.Empty)).Count);
        }

        [TestMethod]
        public void An_empty_keyword_query_offers_an_empty_chat()
        {
            var result = _plugin.Query(new Query(Keyword, Keyword)).Single();

            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Title));

            Assert.IsTrue(result.Action(null));
            Assert.AreEqual("claude://claude.ai/new", _launcher.DeepLinks.Single());
        }

        [TestMethod]
        public void The_context_menu_offers_the_other_destination_and_a_copy()
        {
            var result = _plugin.Query(Typed("hello")).Single();
            var menus = _plugin.LoadContextMenus(result);

            Assert.AreEqual(2, menus.Count);
            Assert.IsTrue(menus[0].Action(null));
            Assert.AreEqual(WebLink, _launcher.BrowserLinks.Single());
            Assert.AreEqual(0, _launcher.DeepLinks.Count);
        }

        [TestMethod]
        public void The_context_menu_of_a_browser_result_offers_the_app()
        {
            _plugin.UpdateSettings(SettingsFactory.Options(options =>
                options.SetChoice(ClaudeSettings.OpenInKey, (int)ClaudeOpenIn.DefaultBrowser)));

            var result = _plugin.Query(Typed("hello")).Single();
            var menus = _plugin.LoadContextMenus(result);

            Assert.AreEqual(2, menus.Count);
            Assert.IsTrue(menus[0].Action(null));
            Assert.AreEqual(AppLink, _launcher.DeepLinks.Single());
        }

        [TestMethod]
        public void Every_context_menu_entry_is_complete()
        {
            var result = _plugin.Query(Typed("hello")).Single();

            foreach (var menu in _plugin.LoadContextMenus(result))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(menu.Title));
                Assert.IsFalse(string.IsNullOrWhiteSpace(menu.Glyph));
                Assert.IsFalse(string.IsNullOrWhiteSpace(menu.FontFamily));
                Assert.AreEqual(_plugin.Name, menu.PluginName);
            }
        }

        [TestMethod]
        public void The_context_menu_is_empty_for_a_result_it_did_not_build()
        {
            Assert.AreEqual(0, _plugin.LoadContextMenus(new Result()).Count);
            Assert.AreEqual(0, _plugin.LoadContextMenus(null).Count);
        }

        [TestMethod]
        public void The_plugin_reports_a_name_and_a_description()
        {
            // Both follow the interface language, so the culture has to be pinned.
            using var culture = new CultureScope("en");

            Assert.AreEqual("New Chat for Claude", _plugin.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(_plugin.Description));
        }

        [TestMethod]
        public void Results_follow_the_interface_language()
        {
            var original = Thread.CurrentThread.CurrentUICulture;

            try
            {
                var english = SubTitleIn("en");
                var french = SubTitleIn("fr");
                var japanese = SubTitleIn("ja");

                Assert.AreNotEqual(english, french, "French did not resolve to its own translation.");
                Assert.AreNotEqual(english, japanese, "Japanese did not resolve to its own translation.");
                Assert.AreNotEqual(french, japanese);
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = original;
            }
        }

        [TestMethod]
        public void A_regional_variant_uses_its_parent_language()
        {
            var original = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Assert.AreEqual(SubTitleIn("fr"), SubTitleIn("fr-CA"));
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = original;
            }
        }

        [TestMethod]
        public void A_language_with_no_translation_falls_back_to_english()
        {
            var original = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Assert.AreEqual(SubTitleIn("en"), SubTitleIn("is"));
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = original;
            }
        }

        [TestMethod]
        public void Reloading_without_a_plugin_context_does_nothing()
        {
            _plugin.ReloadData();
        }

        [TestMethod]
        public void Null_arguments_are_rejected()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Main(null));
            Assert.ThrowsException<ArgumentNullException>(() => _plugin.Query(null));
            Assert.ThrowsException<ArgumentNullException>(() => _plugin.Init(null));
        }

        [TestMethod]
        public void The_default_command_is_answered_with_or_without_a_space()
        {
            // "cl:" cannot be the start of a word, so anything may follow it.
            foreach (var typed in new[] { "cl: hello", "cl:hello" })
            {
                var result = _plugin.Query(new Query(typed, Keyword)).Single();

                Assert.IsTrue(result.Action(null));
                Assert.AreEqual(AppLink, _launcher.DeepLinks.Single(), typed);

                _launcher.DeepLinks.Clear();
            }
        }

        [TestMethod]
        public void A_command_that_can_start_a_word_is_only_answered_on_a_boundary()
        {
            // A command ending in a letter is only a command when a boundary follows
            // it, otherwise "claudette" would be read as a prompt of "tte".
            foreach (var (typed, expected) in new[]
            {
                ("claude", "claude://claude.ai/new"),
                ("claude hello", AppLink),
            })
            {
                var result = _plugin.Query(new Query(typed, "claude")).Single();

                Assert.IsTrue(result.Action(null));
                Assert.AreEqual(expected, _launcher.DeepLinks.Single(), typed);

                _launcher.DeepLinks.Clear();
            }
        }

        [TestMethod]
        public void A_word_swallowed_by_the_command_is_explained_rather_than_answered()
        {
            // PowerToys has already hidden every other plugin for this query, so an
            // empty list would look like a broken launcher.
            foreach (var typed in new[] { "clipboard", "class Foo", "claudette" })
            {
                var command = typed.StartsWith("claude", StringComparison.Ordinal) ? "claude" : "cl";
                var result = _plugin.Query(new Query(typed, command)).Single();

                Assert.IsTrue(result.Title.Contains(command, StringComparison.Ordinal), typed);
                Assert.IsTrue(result.SubTitle.Contains(command + ":", StringComparison.Ordinal), typed);

                Assert.IsTrue(
                    result.IcoPath.EndsWith("warning.png", StringComparison.Ordinal),
                    "The warning must not look like an ordinary result.");

                Assert.IsFalse(result.Action(null), "The warning must not open anything.");
                Assert.AreEqual(0, _launcher.DeepLinks.Count);
                Assert.AreEqual(0, _launcher.BrowserLinks.Count);
            }
        }

        [TestMethod]
        public void The_warning_carries_no_context_menu()
        {
            var warning = _plugin.Query(new Query("clipboard", "cl")).Single();

            Assert.AreEqual(0, _plugin.LoadContextMenus(warning).Count);
        }

        private static Query Typed(string prompt) => new Query(Keyword + " " + prompt, Keyword);

        private static Query Global(string prompt) => new Query(prompt);

        private string SubTitleIn(string culture)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

            return _plugin.Query(new Query(Keyword, Keyword)).Single().SubTitle;
        }
    }
}
