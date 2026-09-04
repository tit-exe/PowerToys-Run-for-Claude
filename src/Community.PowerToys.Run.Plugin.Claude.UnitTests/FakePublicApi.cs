using System.Collections.Generic;

using Common.UI;
using ManagedCommon;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    /// <summary>
    /// Stands in for the launcher. Only the theme event and the message box are used by
    /// the plugin; the rest of the interface is present because it has to be.
    /// </summary>
    internal sealed class FakePublicApi : IPublicAPI
    {
        private readonly List<ThemeChangedHandler> _handlers = new List<ThemeChangedHandler>();

        public event ThemeChangedHandler ThemeChanged
        {
            add => _handlers.Add(value);
            remove => _handlers.Remove(value);
        }

        /// <summary>Gets how many handlers are currently subscribed to the theme event.</summary>
        internal int ThemeSubscribers => _handlers.Count;

        /// <summary>Gets the messages the plugin asked the launcher to show.</summary>
        internal List<string> Messages { get; } = new List<string>();

        /// <summary>Gets or sets the theme reported to the plugin.</summary>
        internal Theme Theme { get; set; } = Theme.Dark;

        public Theme GetCurrentTheme() => Theme;

        public void ShowMsg(string title, string subTitle = "", string iconPath = "", bool useMainWindowAsOwner = true) =>
            Messages.Add(subTitle);

        public void ChangeQuery(string query, bool requery = false)
        {
        }

        public void RemoveUserSelectedItem(Result result)
        {
        }

        public void SaveAppAllSettings()
        {
        }

        public void ReloadAllPluginData()
        {
        }

        public void CheckForNewUpdate()
        {
        }

        public List<PluginPair> GetAllPlugins() => new List<PluginPair>();

        public void ShowNotification(string text, string secondaryText = null)
        {
        }

        /// <summary>
        /// Raises the theme event the way PowerToys does when the system theme changes.
        /// </summary>
        /// <param name="theme">The theme to switch to.</param>
        internal void RaiseThemeChanged(Theme theme)
        {
            foreach (var handler in _handlers.ToArray())
            {
                handler(Theme, theme);
            }

            Theme = theme;
        }
    }
}
