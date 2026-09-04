using System.Linq;

using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    [TestClass]
    public class ClaudeSettingsTests
    {
        [TestMethod]
        public void Missing_settings_leave_the_defaults_in_place()
        {
            var settings = new ClaudeSettings();
            settings.Update(null);

            Assert.AreEqual(ClaudeOpenIn.DesktopApp, settings.OpenIn);
        }

        [TestMethod]
        public void A_settings_file_from_another_version_still_loads()
        {
            var settings = new ClaudeSettings();
            settings.Update(new PowerLauncherPluginSettings
            {
                AdditionalOptions = new[]
                {
                    new PluginAdditionalOption { Key = "SomethingRemoved", Value = false },
                },
            });

            Assert.AreEqual(ClaudeOpenIn.DesktopApp, settings.OpenIn);
        }

        [TestMethod]
        public void Every_declared_option_is_complete_and_unique()
        {
            var options = ClaudeSettings.AdditionalOptions.ToList();

            Assert.AreEqual(1, options.Count);
            Assert.AreEqual(options.Count, options.Select(option => option.Key).Distinct().Count());

            foreach (var option in options)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.Key));
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.DisplayLabel), option.Key);
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.DisplayDescription), option.Key);
            }
        }

        [TestMethod]
        public void The_dropdown_offers_one_item_per_destination()
        {
            var dropdown = ClaudeSettings.AdditionalOptions
                .Single(option => option.Key == ClaudeSettings.OpenInKey);

            Assert.AreEqual(3, dropdown.ComboBoxItems.Count);

            for (var index = 0; index < dropdown.ComboBoxItems.Count; index++)
            {
                var item = dropdown.ComboBoxItems[index];

                Assert.IsFalse(string.IsNullOrWhiteSpace(item.Key));
                Assert.AreEqual(index.ToString(System.Globalization.CultureInfo.InvariantCulture), item.Value);
            }
        }

        [TestMethod]
        public void The_destination_comes_from_the_dropdown()
        {
            foreach (var destination in new[] { ClaudeOpenIn.DesktopApp, ClaudeOpenIn.DefaultBrowser, ClaudeOpenIn.Both })
            {
                var settings = SettingsFactory.Create(options =>
                    options.SetChoice(ClaudeSettings.OpenInKey, (int)destination));

                Assert.AreEqual(destination, settings.OpenIn);
            }
        }

        [TestMethod]
        public void An_out_of_range_destination_falls_back()
        {
            foreach (var stored in new[] { -1, 3, 42 })
            {
                var settings = SettingsFactory.Create(options =>
                    options.SetChoice(ClaudeSettings.OpenInKey, stored));

                Assert.AreEqual(ClaudeOpenIn.DesktopApp, settings.OpenIn);
            }
        }
    }
}
