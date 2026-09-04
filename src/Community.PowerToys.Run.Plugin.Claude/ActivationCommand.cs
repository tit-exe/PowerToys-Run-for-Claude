using System;

using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Claude
{
    /// <summary>
    /// The text a user types to reach this plugin, and the rules PowerToys imposes on it.
    /// </summary>
    /// <remarks>
    /// PowerToys matches an activation command as a plain string prefix, with no notion
    /// of a word boundary, and it hides every global plugin as soon as one matches. A
    /// command ending in a letter therefore swallows ordinary words: with "cl", typing
    /// "clipboard" reaches this plugin and the user loses their programs and files for
    /// that query. Ending the command with a character that cannot continue a word
    /// avoids the problem entirely, which is why the shipped default is "cl:".
    /// </remarks>
    public static class ActivationCommand
    {
        /// <summary>The command declared in <c>plugin.json</c>.</summary>
        public const string Default = "cl:";

        /// <summary>The character that turns a risky command into a safe one.</summary>
        public const char Terminator = ':';

        /// <summary>
        /// Tells whether a command can be read as the beginning of an ordinary word.
        /// </summary>
        /// <param name="command">The command PowerToys has stored.</param>
        /// <returns>True when the command ends in a letter or a digit.</returns>
        public static bool SwallowsWords(string command) =>
            !string.IsNullOrEmpty(command) && char.IsLetterOrDigit(command[command.Length - 1]);

        /// <summary>
        /// Gets a command that starts the same way but cannot swallow words.
        /// </summary>
        /// <param name="command">The command PowerToys has stored.</param>
        /// <returns>The command followed by <see cref="Terminator"/>.</returns>
        public static string Safer(string command) => command + Terminator;

        /// <summary>
        /// Reads the prompt out of a query.
        /// </summary>
        /// <param name="query">The query PowerToys handed over.</param>
        /// <param name="prompt">The text to prefill, trimmed. Never null.</param>
        /// <returns>
        /// False when a word swallowing command matched the middle of a word, which
        /// means the user was typing something else entirely.
        /// </returns>
        public static bool TryReadPrompt(Query query, out string prompt)
        {
            ArgumentNullException.ThrowIfNull(query);

            prompt = (query.Search ?? string.Empty).Trim();

            var command = query.ActionKeyword;

            // No command at all means the plugin is answering a global query, and a
            // command that cannot swallow words needs no further checking.
            if (string.IsNullOrEmpty(command) || !SwallowsWords(command))
            {
                return true;
            }

            var typed = query.RawQuery ?? string.Empty;

            // The command on its own, or followed by a space, was typed deliberately.
            return typed.Length <= command.Length || char.IsWhiteSpace(typed[command.Length]);
        }
    }
}
