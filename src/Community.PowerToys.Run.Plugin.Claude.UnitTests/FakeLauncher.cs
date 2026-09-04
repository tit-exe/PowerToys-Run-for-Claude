using System.Collections.Generic;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    /// <summary>
    /// Records what a result asked to open instead of opening anything.
    /// </summary>
    internal sealed class FakeLauncher : IClaudeLauncher
    {
        public List<string> DeepLinks { get; } = new List<string>();

        public List<string> BrowserLinks { get; } = new List<string>();

        public bool DeepLinkSucceeds { get; set; } = true;

        public bool BrowserSucceeds { get; set; } = true;

        public bool OpenDeepLink(string uri)
        {
            DeepLinks.Add(uri);
            return DeepLinkSucceeds;
        }

        public bool OpenInBrowser(string url)
        {
            BrowserLinks.Add(url);
            return BrowserSucceeds;
        }
    }
}
