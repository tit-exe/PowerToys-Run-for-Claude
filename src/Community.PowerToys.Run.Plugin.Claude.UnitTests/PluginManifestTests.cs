using System;
using System.IO;
using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    /// <summary>
    /// Guards the files PowerToys reads before any code runs. A manifest that does not
    /// parse, or that disagrees with the assembly, makes the plugin vanish from the
    /// launcher with no error the user can see.
    /// </summary>
    [TestClass]
    public class PluginManifestTests
    {
        private const string ProjectName = "Community.PowerToys.Run.Plugin.Claude";

        private static JsonElement _manifest;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _ = context;

            var path = Repository.InProject("plugin.json");

            Assert.IsTrue(File.Exists(path), $"plugin.json not found at {path}.");

            // PowerToys reads the manifest itself and refuses a plugin whose JSON it
            // cannot parse, silently. Parsing it here turns that into a failing test.
            _manifest = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        }

        [TestMethod]
        public void The_identifier_matches_the_plugin()
        {
            Assert.AreEqual(Main.PluginID, Text("ID"));
            Assert.AreEqual(32, Main.PluginID.Length);
        }

        [TestMethod]
        public void The_assembly_named_in_the_manifest_is_the_one_that_is_built()
        {
            Assert.AreEqual(
                ProjectName + ".dll",
                Text("ExecuteFileName"));

            Assert.AreEqual(ProjectName, typeof(Main).Assembly.GetName().Name);
        }

        [TestMethod]
        public void The_manifest_version_matches_the_assembly()
        {
            // PowerToys shows the manifest version while the launcher loads the
            // assembly, so a mismatch would report a release that is not the one
            // running. The build number is dropped: assemblies always carry four parts.
            var assembly = typeof(Main).Assembly.GetName().Version;
            var version = $"{assembly.Major}.{assembly.Minor}.{assembly.Build}";

            Assert.AreEqual(version, Text("Version"));
        }

        [TestMethod]
        public void Both_icons_exist()
        {
            foreach (var key in new[] { "IcoPathDark", "IcoPathLight" })
            {
                var relative = Text(key).Replace('\\', Path.DirectorySeparatorChar);
                var path = Repository.InProject(relative);

                Assert.IsTrue(File.Exists(path), $"{key} points at {path}, which does not exist.");
            }
        }

        [TestMethod]
        public void The_icons_the_code_asks_for_are_the_ones_that_ship()
        {
            // Main names these three paths directly: one mark per PowerToys theme,
            // and the warning shown when the activation command swallowed a word.
            foreach (var name in new[] { "newchat.light.png", "newchat.dark.png", "warning.png" })
            {
                var path = Repository.InProject("Images", name);

                Assert.IsTrue(File.Exists(path), $"{name} is missing.");
            }
        }

        [TestMethod]
        public void The_manifest_description_matches_the_neutral_resource()
        {
            // PowerToys Settings reads this string straight from disk, so it cannot
            // follow the interface language. Keeping it equal to the neutral resource
            // stops the two from drifting apart.
            using var culture = new CultureScope("en");
            using var plugin = new Main(new FakeLauncher());

            Assert.AreEqual(plugin.Description, Text("Description"));
            Assert.AreEqual(plugin.Name, Text("Name"));
        }

        [TestMethod]
        public void The_shipped_activation_command_is_the_one_the_code_documents()
        {
            Assert.AreEqual(ActivationCommand.Default, Text("ActionKeyword"));
            Assert.IsFalse(
                ActivationCommand.SwallowsWords(Text("ActionKeyword")),
                "The shipped command would hide other plugins for ordinary words.");
        }

        [TestMethod]
        public void The_manifest_carries_what_the_launcher_needs()
        {
            Assert.AreEqual("New Chat for Claude", Text("Name"));
            Assert.AreEqual("csharp", Text("Language"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Text("Author")));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Text("Description")));
            Assert.IsTrue(Uri.IsWellFormedUriString(Text("Website"), UriKind.Absolute));
        }

        private static string Text(string property)
        {
            Assert.IsTrue(_manifest.TryGetProperty(property, out var value), $"plugin.json has no '{property}'.");

            return value.GetString();
        }
    }
}
