using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    [TestClass]
    public class ActivationCommandTests
    {
        [TestMethod]
        public void A_command_ending_in_a_letter_or_digit_swallows_words()
        {
            foreach (var command in new[] { "cl", "claude", "c", "cl2" })
            {
                Assert.IsTrue(ActivationCommand.SwallowsWords(command), command);
            }
        }

        [TestMethod]
        public void A_command_ending_in_a_symbol_does_not()
        {
            foreach (var command in new[] { "cl:", "cl.", "cl-", "//", "$$", "?" })
            {
                Assert.IsFalse(ActivationCommand.SwallowsWords(command), command);
            }
        }

        [TestMethod]
        public void An_absent_command_swallows_nothing()
        {
            Assert.IsFalse(ActivationCommand.SwallowsWords(null));
            Assert.IsFalse(ActivationCommand.SwallowsWords(string.Empty));
        }

        [TestMethod]
        public void The_shipped_default_cannot_swallow_words()
        {
            Assert.IsFalse(ActivationCommand.SwallowsWords(ActivationCommand.Default));
        }

        [TestMethod]
        public void The_suggestion_fixes_a_command_that_swallows_words()
        {
            foreach (var command in new[] { "cl", "claude", "c" })
            {
                var safer = ActivationCommand.Safer(command);

                Assert.IsTrue(safer.StartsWith(command, System.StringComparison.Ordinal));
                Assert.IsFalse(ActivationCommand.SwallowsWords(safer), safer);
            }
        }

        [TestMethod]
        public void A_safe_command_accepts_anything_after_it()
        {
            foreach (var typed in new[] { "cl:", "cl:hello", "cl: hello" })
            {
                Assert.IsTrue(ActivationCommand.TryReadPrompt(new Query(typed, "cl:"), out _), typed);
            }
        }

        [TestMethod]
        public void A_safe_command_reads_the_prompt_with_or_without_a_space()
        {
            ActivationCommand.TryReadPrompt(new Query("cl:hello", "cl:"), out var withoutSpace);
            ActivationCommand.TryReadPrompt(new Query("cl: hello", "cl:"), out var withSpace);

            Assert.AreEqual("hello", withoutSpace);
            Assert.AreEqual("hello", withSpace);
        }

        [TestMethod]
        public void A_word_swallowing_command_needs_a_boundary()
        {
            Assert.IsTrue(ActivationCommand.TryReadPrompt(new Query("cl", "cl"), out _));
            Assert.IsTrue(ActivationCommand.TryReadPrompt(new Query("cl hello", "cl"), out _));
            Assert.IsFalse(ActivationCommand.TryReadPrompt(new Query("clipboard", "cl"), out _));
            Assert.IsFalse(ActivationCommand.TryReadPrompt(new Query("clean up", "cl"), out _));
        }

        [TestMethod]
        public void A_global_query_is_always_read()
        {
            Assert.IsTrue(ActivationCommand.TryReadPrompt(new Query("anything at all"), out var prompt));
            Assert.AreEqual("anything at all", prompt);
        }

        [TestMethod]
        public void The_prompt_is_trimmed()
        {
            ActivationCommand.TryReadPrompt(new Query("cl:   hello  ", "cl:"), out var prompt);

            Assert.AreEqual("hello", prompt);
        }
    }
}
