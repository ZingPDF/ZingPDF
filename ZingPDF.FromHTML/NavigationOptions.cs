using PuppeteerSharp;

namespace ZingPDF.FromHTML
{
    public class NavigationOptions
    {
        private NavigationOptions() { }

        /// <summary>
        /// When to consider navigation succeeded, defaults to <see cref="WaitUntilNavigation.Load"/>. 
        /// This is a bit flag field, navigation is considered to be successful after all events have been fired.</param>
        /// </summary>
        public WaitUntil? WaitUntilFlags { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int? TimeoutExpiration { get; private set; }

        public static NavigationOptions Default { get; } = new NavigationOptions();

        public static NavigationOptions Timeout(int seconds) => new() { TimeoutExpiration = seconds };
        public static NavigationOptions WaitUntil(WaitUntil waitUntil) => new() { WaitUntilFlags = waitUntil };
    }
}
