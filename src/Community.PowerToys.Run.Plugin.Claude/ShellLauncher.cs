using System;
using System.ComponentModel;
using System.Diagnostics;

using Wox.Infrastructure;
using Wox.Plugin.Logger;

using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;

namespace Community.PowerToys.Run.Plugin.Claude
{
    /// <summary>
    /// Opens links through the shell.
    /// </summary>
    /// <remarks>
    /// Deep links are handed to the shell so Windows can route them to whatever is
    /// registered, including a packaged app. Web links go through the browser PowerToys
    /// resolved, which keeps the plugin consistent with the built-in ones and honours a
    /// private browsing pattern.
    /// </remarks>
    public sealed class ShellLauncher : IClaudeLauncher
    {
        /// <inheritdoc/>
        public bool OpenDeepLink(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return false;
            }

            try
            {
                var process = Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
                process?.Dispose();
                return true;
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
            {
                LogFailure(link, exception);
                return false;
            }
        }

        /// <inheritdoc/>
        public bool OpenInBrowser(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return false;
            }

            // PowerToys fills this in once it has resolved the default browser. Its
            // shell helper only guards against Win32 failures, so an unresolved path
            // would surface as an unhandled InvalidOperationException.
            if (string.IsNullOrWhiteSpace(BrowserInfo.Path))
            {
                Log.Warn("No default browser has been resolved yet.", typeof(ShellLauncher));
                return false;
            }

            return Helper.OpenCommandInShell(BrowserInfo.Path, BrowserInfo.ArgumentsPattern, link);
        }

        /// <summary>
        /// Records a failure without the query string, which holds whatever the user typed.
        /// </summary>
        private static void LogFailure(string link, Exception exception)
        {
            var separator = link.IndexOf('?');
            var route = separator < 0 ? link : link.Substring(0, separator);

            Log.Exception($"Could not open '{route}'.", exception, typeof(ShellLauncher));
        }
    }
}
