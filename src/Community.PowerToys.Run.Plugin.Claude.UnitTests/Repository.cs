using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    /// <summary>
    /// Finds the files a test needs to read from the repository rather than from the
    /// build output, so a test fails on the source of truth.
    /// </summary>
    internal static class Repository
    {
        private const string ProjectName = "Community.PowerToys.Run.Plugin.Claude";

        /// <summary>The file that marks the repository root.</summary>
        private const string SolutionFile = "NewChatForClaude.sln";

        /// <summary>Gets the folder holding the plugin project.</summary>
        internal static DirectoryInfo ProjectFolder { get; } =
            new DirectoryInfo(Path.Combine(Root().FullName, "src", ProjectName));

        /// <summary>Builds a path inside the plugin project.</summary>
        /// <param name="segments">Path segments below the project folder.</param>
        /// <returns>An absolute path.</returns>
        internal static string InProject(params string[] segments) =>
            Path.Combine(ProjectFolder.FullName, Path.Combine(segments));

        private static DirectoryInfo Root()
        {
            var folder = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            while (folder is not null && !File.Exists(Path.Combine(folder.FullName, SolutionFile)))
            {
                folder = folder.Parent;
            }

            Assert.IsNotNull(folder, "Could not find the repository root from the test assembly.");

            return folder;
        }
    }
}
