namespace Community.PowerToys.Run.Plugin.Claude
{
    /// <summary>
    /// Opens the links a result points at.
    /// </summary>
    public interface IClaudeLauncher
    {
        /// <summary>
        /// Hands <paramref name="link"/> to the registered protocol handler.
        /// </summary>
        /// <param name="link">A claude:// link.</param>
        /// <returns>True when the handler was started.</returns>
        bool OpenDeepLink(string link);

        /// <summary>
        /// Opens <paramref name="link"/> in the user's default browser.
        /// </summary>
        /// <param name="link">An http(s) address.</param>
        /// <returns>True when the browser was started.</returns>
        bool OpenInBrowser(string link);
    }
}
