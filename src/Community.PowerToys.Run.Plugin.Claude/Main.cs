using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Plugin;

using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;

namespace Community.PowerToys.Run.Plugin.Claude
{
    /// <summary>
    /// Turns a PowerToys Run query into a new Claude conversation, opened either in the
    /// desktop app or in the browser.
    /// </summary>
    public class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IReloadable, IDisposable
    {
        private const int KeywordScore = 100;

        /// <summary>
        /// Score for results shown without the activation command. Kept lower so the
        /// plugin never outranks a program or file the user was looking for.
        /// </summary>
        private const int GlobalScore = 60;

        /// <summary>
        /// Shortest query answered without the activation command. Below this, what the
        /// user is typing is far more likely to be the start of a file or program name,
        /// and a result here would only be in the way.
        /// </summary>
        private const int MinimumGlobalLength = 3;

        private const string LightThemeIconPath = "Images/newchat.light.png";
        private const string DarkThemeIconPath = "Images/newchat.dark.png";

        /// <summary>
        /// Shown instead of the plugin icon when the activation command swallowed a
        /// word. PowerToys Run offers no way to colour a result, so the icon is the only
        /// place a warning can be made to look like one.
        /// </summary>
        private const string WarningIconPath = "Images/warning.png";

        private const string CopyGlyph = "\xE8C8";
        private const string BrowserGlyph = "\xE774";
        private const string AppGlyph = "\xE8A7";
        private const string GlyphFontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets";

        private readonly ClaudeSettings _settings = new ClaudeSettings();
        private readonly IClaudeLauncher _launcher;

        private PluginInitContext _context;
        private string _iconPath;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Main"/> class. PowerToys uses
        /// this constructor.
        /// </summary>
        public Main()
            : this(new ShellLauncher())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Main"/> class with an explicit
        /// launcher, which lets tests exercise <see cref="Query"/> without opening
        /// anything.
        /// </summary>
        /// <param name="launcher">Opens the links behind a result.</param>
        public Main(IClaudeLauncher launcher) =>
            _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));

        /// <summary>Gets the identifier PowerToys uses for this plugin.</summary>
        public static string PluginID => "F05FADA4EC2C42B088986DA0500C25EF";

        /// <summary>
        /// Gets the plugin name the launcher shows, read from the strings so it follows
        /// the interface language.
        /// </summary>
        public string Name => Strings.PluginName;

        /// <summary>
        /// Gets the plugin description the launcher shows, read from the strings so it
        /// follows the interface language.
        /// </summary>
        public string Description => Strings.PluginDescription;

        /// <inheritdoc/>
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => ClaudeSettings.AdditionalOptions;

        /// <inheritdoc/>
        public string GetTranslatedPluginTitle() => Name;

        /// <inheritdoc/>
        public string GetTranslatedPluginDescription() => Description;

        /// <summary>
        /// Gets the display name of the browser PowerToys resolved, falling back to the
        /// name it uses when no default is registered.
        /// </summary>
        private static string BrowserName => BrowserInfo.Name ?? BrowserInfo.MSEdgeName;

        /// <inheritdoc/>
        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;

            UpdateIconPath(_context.API.GetCurrentTheme());
            BrowserInfo.UpdateIfTimePassed();
        }

        /// <inheritdoc/>
        public List<Result> Query(Query query)
        {
            ArgumentNullException.ThrowIfNull(query);

            if (!ActivationCommand.TryReadPrompt(query, out var prompt))
            {
                return new List<Result> { CreateAmbiguousCommandWarning(query.ActionKeyword) };
            }

            var isGlobal = string.IsNullOrEmpty(query.ActionKeyword);

            if (prompt.Length == 0)
            {
                // Without an activation command an empty query belongs to other plugins.
                return isGlobal ? new List<Result>() : CreateResults(string.Empty, isGlobal: false);
            }

            if (isGlobal && prompt.Length < MinimumGlobalLength)
            {
                return new List<Result>();
            }

            return CreateResults(prompt, isGlobal);
        }

        /// <inheritdoc/>
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            var menus = new List<ContextMenuResult>(2);

            if (selectedResult?.ContextData is not ClaudeResultContext context)
            {
                return menus;
            }

            // The result's own action covers one destination; offer the other one here.
            var alternateOpensApp = !context.OpensApp;

            menus.Add(CreateContextMenu(
                alternateOpensApp ? Strings.ContextOpenInApp : Strings.ContextOpenInBrowser,
                alternateOpensApp ? AppGlyph : BrowserGlyph,
                Key.Enter,
                () => Open(alternateOpensApp, context.AlternateLink)));

            menus.Add(CreateContextMenu(
                Strings.ContextCopyLink,
                CopyGlyph,
                Key.C,
                () => CopyToClipboard(context.PrimaryLink)));

            return menus;
        }

        /// <inheritdoc/>
        public Control CreateSettingPanel() => throw new NotImplementedException();

        /// <inheritdoc/>
        public void UpdateSettings(PowerLauncherPluginSettings settings) => _settings.Update(settings);

        /// <inheritdoc/>
        public void ReloadData()
        {
            if (_context is null)
            {
                return;
            }

            UpdateIconPath(_context.API.GetCurrentTheme());
            BrowserInfo.UpdateIfTimePassed();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the plugin's event subscriptions.
        /// </summary>
        /// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
            {
                return;
            }

            if (_context?.API is not null)
            {
                _context.API.ThemeChanged -= OnThemeChanged;
            }

            _disposed = true;
        }

        private List<Result> CreateResults(string prompt, bool isGlobal)
        {
            var appLink = ClaudeLink.ToApp(prompt);
            var browserLink = ClaudeLink.ToBrowser(prompt);
            var title = prompt.Length == 0 ? Strings.TitleNewChat : prompt;
            var score = isGlobal ? GlobalScore : KeywordScore;

            var results = new List<Result>(2);

            if (_settings.OpenIn != ClaudeOpenIn.DefaultBrowser)
            {
                results.Add(CreateResult(
                    title,
                    Strings.SubtitleInApp,
                    score,
                    new ClaudeResultContext(appLink, browserLink, opensApp: true)));
            }

            if (_settings.OpenIn != ClaudeOpenIn.DesktopApp)
            {
                results.Add(CreateResult(
                    title,
                    Format(Strings.SubtitleInBrowser, BrowserName),
                    results.Count == 0 ? score : score - 1,
                    new ClaudeResultContext(appLink, browserLink, opensApp: false)));
            }

            return results;
        }

        private Result CreateResult(string title, string subTitle, int score, ClaudeResultContext context) =>
            new Result
            {
                Title = title,
                SubTitle = subTitle,
                QueryTextDisplay = title,
                IcoPath = _iconPath,
                Score = score,
                ContextData = context,
                Action = _ => Open(context.OpensApp, context.PrimaryLink),
            };

        /// <summary>
        /// Builds the one result shown when the activation command swallowed a word the
        /// user meant for something else. PowerToys has already hidden every other
        /// plugin for this query, so an empty list would look like a broken launcher.
        /// </summary>
        private static Result CreateAmbiguousCommandWarning(string command) =>
            new Result
            {
                Title = Format(Strings.WarningAmbiguousCommandTitle, command),
                SubTitle = Format(Strings.WarningAmbiguousCommandDescription, ActivationCommand.Safer(command)),
                QueryTextDisplay = string.Empty,
                IcoPath = WarningIconPath,
                Score = KeywordScore,

                // Nothing to open. Leaving the window up lets the message be read.
                Action = _ => false,
            };

        private ContextMenuResult CreateContextMenu(string title, string glyph, Key key, Func<bool> action) =>
            new ContextMenuResult
            {
                PluginName = Name,
                Title = title,
                Glyph = glyph,
                FontFamily = GlyphFontFamily,
                AcceleratorKey = key,
                AcceleratorModifiers = ModifierKeys.Control,
                Action = _ => action(),
            };

        /// <summary>
        /// Opens a link in one of the two destinations.
        /// </summary>
        /// <param name="inApp">True for the desktop app, false for the browser.</param>
        /// <param name="link">The link that destination understands.</param>
        /// <returns>True when the destination was opened.</returns>
        private bool Open(bool inApp, string link) => inApp ? OpenInApp(link) : OpenInBrowser(link);

        /// <summary>
        /// Opens the desktop app.
        /// </summary>
        /// <remarks>
        /// The link is handed to the shell without checking first whether anything
        /// handles it. Claude ships as a packaged app, and neither the association APIs
        /// nor the registry report its scheme reliably from every elevation, while the
        /// launch itself works. A check would therefore refuse links that do open.
        /// </remarks>
        private bool OpenInApp(string link)
        {
            if (_launcher.OpenDeepLink(link))
            {
                return true;
            }

            ShowMessage(Strings.ErrorAppLaunch);
            return false;
        }

        private bool OpenInBrowser(string link)
        {
            if (_launcher.OpenInBrowser(link))
            {
                return true;
            }

            ShowMessage(Format(Strings.ErrorBrowserLaunch, BrowserName));
            return false;
        }

        private bool CopyToClipboard(string link)
        {
            try
            {
                // copy: true flushes the data to the clipboard, so the link survives
                // PowerToys shutting down.
                Clipboard.SetDataObject(link, copy: true);
                return true;
            }
            catch (ExternalException)
            {
                // Another process owns the clipboard.
                ShowMessage(Strings.ErrorClipboard);
                return false;
            }
        }

        /// <summary>
        /// Fills the single placeholder of a localized string.
        /// </summary>
        [SuppressMessage(
            "Performance",
            "CA1863:Use CompositeFormat",
            Justification = "The format follows the interface language, so it cannot be parsed once and cached.")]
        private static string Format(string format, string argument) =>
            string.Format(CultureInfo.CurrentCulture, format, argument);

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);

        /// <summary>
        /// Points at the icon the current theme needs. The mark is a single colour
        /// glyph, so it has to be dark on a light theme and light on a dark one.
        /// </summary>
        private void UpdateIconPath(Theme theme) =>
            _iconPath = theme == Theme.Light || theme == Theme.HighContrastWhite
                ? LightThemeIconPath
                : DarkThemeIconPath;

        private void ShowMessage(string message) =>
            _context?.API?.ShowMsg(Strings.PluginName, message, _iconPath);
    }
}
