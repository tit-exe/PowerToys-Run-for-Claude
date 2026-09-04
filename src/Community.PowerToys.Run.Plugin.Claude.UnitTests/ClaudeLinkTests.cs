using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    [TestClass]
    public class ClaudeLinkTests
    {
        [TestMethod]
        public void A_prompt_is_percent_encoded()
        {
            Assert.AreEqual(
                "claude://claude.ai/new?q=what%20is%20the%20date%20today%3F",
                ClaudeLink.ToApp("what is the date today?"));

            Assert.AreEqual(
                "https://claude.ai/new?q=what%20is%20the%20date%20today%3F",
                ClaudeLink.ToBrowser("what is the date today?"));
        }

        [TestMethod]
        public void An_empty_prompt_leaves_the_route_bare()
        {
            foreach (var prompt in new[] { null, string.Empty, "   " })
            {
                Assert.AreEqual("claude://claude.ai/new", ClaudeLink.ToApp(prompt));
                Assert.AreEqual("https://claude.ai/new", ClaudeLink.ToBrowser(prompt));
            }
        }

        [TestMethod]
        public void Surrounding_whitespace_is_dropped()
        {
            Assert.AreEqual("claude://claude.ai/new?q=hello", ClaudeLink.ToApp("  hello  "));
        }

        [TestMethod]
        public void Reserved_characters_survive_a_round_trip()
        {
            const string Prompt = "a&b=c?d/e#f+g%h \"i\" 'j' <k>";

            Assert.AreEqual(Prompt, Decode(ClaudeLink.ToApp(Prompt)));
        }

        [TestMethod]
        public void Text_in_any_script_survives_a_round_trip()
        {
            foreach (var prompt in new[]
            {
                "quelle est la date d'aujourd'hui",
                "Wie ist das Wetter in Muenchen?",
                "今日の日付は？",
                "Какая сегодня дата",
                "ما هو تاريخ اليوم",
                "오늘 날짜가 언제인지",
            })
            {
                Assert.AreEqual(prompt, Decode(ClaudeLink.ToApp(prompt)), prompt);
            }
        }

        [TestMethod]
        public void A_long_prompt_stays_within_the_command_line_ceiling()
        {
            var link = ClaudeLink.ToApp(new string('a', 50000));

            Assert.IsTrue(link.Length <= ClaudeLink.MaxLinkLength, $"Link was {link.Length} characters.");
        }

        [TestMethod]
        public void A_prompt_of_astral_characters_is_never_cut_mid_character()
        {
            // Each of these takes two UTF-16 units and twelve characters once encoded,
            // so the budget runs out in the middle of the sequence.
            var link = ClaudeLink.ToApp(string.Concat(Enumerable.Repeat("\U0001F600", 4000)));

            Assert.IsTrue(link.Length <= ClaudeLink.MaxLinkLength);

            var decoded = Decode(link);

            Assert.AreEqual(0, decoded.Length % 2, "A surrogate pair was split.");
            Assert.IsFalse(decoded.Contains('�'), "The prompt contains a replacement character.");
        }

        [TestMethod]
        public void A_prompt_is_cut_at_the_length_the_composer_accepts()
        {
            var link = ClaudeLink.ToApp(new string('é', ClaudeLink.MaxPromptLength + 500));

            // Each of these encodes to six characters, so the link ceiling binds first.
            Assert.IsTrue(link.Length <= ClaudeLink.MaxLinkLength);
            Assert.IsTrue(Decode(link).Length <= ClaudeLink.MaxPromptLength);
        }

        [TestMethod]
        public void The_two_routes_differ_only_by_scheme_and_host()
        {
            Assert.IsTrue(ClaudeLink.AppRoute.StartsWith("claude://", StringComparison.Ordinal));
            Assert.IsTrue(ClaudeLink.BrowserRoute.StartsWith("https://", StringComparison.Ordinal));
            Assert.IsTrue(ClaudeLink.AppRoute.EndsWith("/new", StringComparison.Ordinal));
            Assert.IsTrue(ClaudeLink.BrowserRoute.EndsWith("/new", StringComparison.Ordinal));
        }

        private static string Decode(string link)
        {
            var marker = link.IndexOf("?q=", StringComparison.Ordinal);

            return marker < 0 ? string.Empty : Uri.UnescapeDataString(link.AsSpan(marker + 3));
        }
    }
}
