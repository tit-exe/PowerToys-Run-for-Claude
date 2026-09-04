namespace Community.PowerToys.Run.Plugin.Claude
{
    /// <summary>
    /// Where a result should open Claude. The numeric values are persisted in the
    /// PowerToys settings file, so they must stay stable across releases.
    /// </summary>
    public enum ClaudeOpenIn
    {
        /// <summary>Only offer the desktop app.</summary>
        DesktopApp = 0,

        /// <summary>Only offer the default browser.</summary>
        DefaultBrowser = 1,

        /// <summary>Offer both, desktop app first.</summary>
        Both = 2,
    }
}
