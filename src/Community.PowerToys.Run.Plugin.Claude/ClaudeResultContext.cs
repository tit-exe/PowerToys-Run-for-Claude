namespace Community.PowerToys.Run.Plugin.Claude
{
    /// <summary>
    /// The links behind a result, carried through to the context menu.
    /// </summary>
    public sealed class ClaudeResultContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClaudeResultContext"/> class.
        /// </summary>
        /// <param name="appLink">The deep link for the desktop app.</param>
        /// <param name="browserLink">The address for the browser.</param>
        /// <param name="opensApp">Whether the result's own action opens the app.</param>
        public ClaudeResultContext(string appLink, string browserLink, bool opensApp)
        {
            AppLink = appLink;
            BrowserLink = browserLink;
            OpensApp = opensApp;
        }

        /// <summary>Gets the deep link for the desktop app.</summary>
        public string AppLink { get; }

        /// <summary>Gets the address for the browser.</summary>
        public string BrowserLink { get; }

        /// <summary>
        /// Gets a value indicating whether pressing Enter opens the desktop app. The
        /// context menu offers the other destination.
        /// </summary>
        public bool OpensApp { get; }

        /// <summary>Gets the link the result's own action opens.</summary>
        public string PrimaryLink => OpensApp ? AppLink : BrowserLink;

        /// <summary>Gets the link the context menu offers instead.</summary>
        public string AlternateLink => OpensApp ? BrowserLink : AppLink;
    }
}
