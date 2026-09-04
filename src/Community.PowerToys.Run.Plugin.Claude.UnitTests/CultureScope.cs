using System;
using System.Globalization;
using System.Threading;

namespace Community.PowerToys.Run.Plugin.Claude.UnitTests
{
    /// <summary>
    /// Switches the interface language for the duration of a block and puts the previous
    /// one back, so one test cannot leave a language behind for the next.
    /// </summary>
    internal sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous;

        internal CultureScope(string culture)
        {
            _previous = Thread.CurrentThread.CurrentUICulture;
            Switch(culture);
        }

        /// <summary>Changes the language again inside the same scope.</summary>
        /// <param name="culture">A culture code such as "fr" or "zh-Hans".</param>
        internal static void Switch(string culture) =>
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

        public void Dispose() => Thread.CurrentThread.CurrentUICulture = _previous;
    }
}
