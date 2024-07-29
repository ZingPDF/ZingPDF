using PuppeteerSharp;

namespace ZingPDF.FromHTML
{
    public class NavigationOptions
    {
        /// <summary>
        /// When to consider navigation succeeded, defaults to <see cref="WaitUntilNavigation.Load"/>. 
        /// This is a bit flag field, navigation is considered to be successful after all events have been fired.</param>
        /// </summary>
        public WaitUntil? WaitUntilFlags { get; set; }

        /// <summary>
        /// Maximum navigation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.
        /// </summary>
        public int TimeoutExpiration { get; set; } = 1000 * 30;

        public static NavigationOptions Default { get; } = new NavigationOptions();

        public static NavigationOptions Timeout(int milliseconds) => new() { TimeoutExpiration = milliseconds };
        public static NavigationOptions WaitUntil(WaitUntil waitUntil) => new() { WaitUntilFlags = waitUntil };
    }
}
