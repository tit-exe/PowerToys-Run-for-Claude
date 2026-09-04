using System;

namespace Community.PowerToys.Run.Plugin.Claude
{
    /// <summary>
    /// Builds the two links a result can open.
    /// </summary>
    public static class ClaudeLink
    {
        /// <summary>Deep link that opens a new chat in the desktop app.</summary>
        public const string AppRoute = "claude://claude.ai/new";

        /// <summary>Address that opens a new chat on the web.</summary>
        public const string BrowserRoute = "https://claude.ai/new";

        /// <summary>Prompt length the composer accepts before it truncates.</summary>
        public const int MaxPromptLength = 14000;

        /// <summary>
        /// Ceiling for the whole percent encoded link. Windows caps a command line at
        /// 8191 characters and the browser argument pattern eats into that, so links are
        /// kept below it rather than failing to launch.
        /// </summary>
        public const int MaxLinkLength = 8000;

        private const string PromptParameter = "?q=";

        /// <summary>
        /// Builds the link that opens <paramref name="prompt"/> in the desktop app.
        /// </summary>
        /// <param name="prompt">The text to prefill; may be null or empty.</param>
        /// <returns>An absolute claude:// link.</returns>
        public static string ToApp(string prompt) => Compose(AppRoute, prompt);

        /// <summary>
        /// Builds the address that opens <paramref name="prompt"/> in a browser.
        /// </summary>
        /// <param name="prompt">The text to prefill; may be null or empty.</param>
        /// <returns>An absolute https address.</returns>
        public static string ToBrowser(string prompt) => Compose(BrowserRoute, prompt);

        private static string Compose(string route, string prompt)
        {
            var text = (prompt ?? string.Empty).Trim();

            if (text.Length == 0)
            {
                return route;
            }

            var budget = MaxLinkLength - route.Length - PromptParameter.Length;
            var encoded = EncodeToFit(Truncate(text, MaxPromptLength), budget);

            return encoded.Length == 0 ? route : route + PromptParameter + encoded;
        }

        /// <summary>
        /// Percent encodes <paramref name="value"/>, shortening it on whole character
        /// boundaries until the encoded form fits in <paramref name="budget"/>.
        /// </summary>
        private static string EncodeToFit(string value, int budget)
        {
            if (budget <= 0)
            {
                return string.Empty;
            }

            var encoded = Uri.EscapeDataString(value);

            if (encoded.Length <= budget)
            {
                return encoded;
            }

            var shortest = 0;
            var longest = value.Length;

            while (shortest < longest)
            {
                var middle = (shortest + longest + 1) / 2;

                if (Uri.EscapeDataString(Truncate(value, middle)).Length <= budget)
                {
                    shortest = middle;
                }
                else
                {
                    longest = middle - 1;
                }
            }

            return shortest == 0 ? string.Empty : Uri.EscapeDataString(Truncate(value, shortest));
        }

        /// <summary>
        /// Keeps the first <paramref name="length"/> characters without splitting a
        /// surrogate pair, which would produce a string that cannot be encoded.
        /// </summary>
        private static string Truncate(string value, int length)
        {
            if (length >= value.Length)
            {
                return value;
            }

            if (length > 0 && char.IsHighSurrogate(value[length - 1]))
            {
                length--;
            }

            return value.Substring(0, length);
        }
    }
}
