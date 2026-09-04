using System;
using System.Linq;

using ManagedCommon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    /// <summary>
    /// Guards what the plugin holds on to. PowerToys keeps its public API alive for the
    /// whole session, so a handler left subscribed would keep the plugin alive with it.
    /// </summary>
    [TestClass]
    public class PluginLifetimeTests
    {
        [TestMethod]
        public void Init_subscribes_once_and_Dispose_unsubscribes()
        {
            var api = new FakePublicApi();
            var plugin = new Main(new FakeLauncher());

            Assert.AreEqual(0, api.ThemeSubscribers);

            plugin.Init(new PluginInitContext { API = api });
            Assert.AreEqual(1, api.ThemeSubscribers);

            plugin.Dispose();
            Assert.AreEqual(0, api.ThemeSubscribers, "The theme handler outlived the plugin.");
        }

        [TestMethod]
        public void Disposing_more_than_once_unsubscribes_only_once()
        {
            var api = new FakePublicApi();
            var plugin = new Main(new FakeLauncher());

            plugin.Init(new PluginInitContext { API = api });

            plugin.Dispose();
            plugin.Dispose();
            plugin.Dispose();

            Assert.AreEqual(0, api.ThemeSubscribers);
        }

        [TestMethod]
        public void Disposing_without_Init_is_harmless()
        {
            var plugin = new Main(new FakeLauncher());

            plugin.Dispose();
        }

        [TestMethod]
        public void The_icon_follows_the_theme()
        {
            var api = new FakePublicApi { Theme = Theme.Dark };
            using var plugin = new Main(new FakeLauncher());

            plugin.Init(new PluginInitContext { API = api });

            var dark = plugin.Query(new Query("cl: hello", "cl:")).Single().IcoPath;

            api.RaiseThemeChanged(Theme.Light);
            var light = plugin.Query(new Query("cl: hello", "cl:")).Single().IcoPath;

            api.RaiseThemeChanged(Theme.HighContrastWhite);
            var highContrast = plugin.Query(new Query("cl: hello", "cl:")).Single().IcoPath;

            // The mark is a single colour glyph, so it is invisible on the wrong ground.
            Assert.AreNotEqual(dark, light, "The icon did not change with the theme.");
            Assert.AreEqual(light, highContrast, "A light high contrast theme should use the light icon.");
            Assert.IsTrue(dark.EndsWith("newchat.dark.png", StringComparison.Ordinal), dark);
            Assert.IsTrue(light.EndsWith("newchat.light.png", StringComparison.Ordinal), light);
        }

        [TestMethod]
        public void A_theme_change_after_Dispose_is_ignored()
        {
            var api = new FakePublicApi { Theme = Theme.Dark };
            var plugin = new Main(new FakeLauncher());

            plugin.Init(new PluginInitContext { API = api });
            plugin.Dispose();

            api.RaiseThemeChanged(Theme.Light);

            Assert.AreEqual(0, api.ThemeSubscribers);
        }

        [TestMethod]
        public void Reloading_refreshes_the_icon()
        {
            var api = new FakePublicApi { Theme = Theme.Dark };
            using var plugin = new Main(new FakeLauncher());

            plugin.Init(new PluginInitContext { API = api });

            api.Theme = Theme.Light;
            plugin.ReloadData();

            Assert.IsTrue(
                plugin.Query(new Query("cl: hello", "cl:")).Single().IcoPath
                    .EndsWith("newchat.light.png", StringComparison.Ordinal));
        }

        [TestMethod]
        public void A_failed_launch_reaches_the_user()
        {
            var api = new FakePublicApi();
            var launcher = new FakeLauncher { DeepLinkSucceeds = false };
            using var plugin = new Main(launcher);

            plugin.Init(new PluginInitContext { API = api });
            plugin.Query(new Query("cl: hello", "cl:")).Single().Action(null);

            Assert.AreEqual(1, api.Messages.Count, "The user was not told the launch failed.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(api.Messages[0]));
        }
    }
}
