namespace PuppeteerSharp.Input
{
    /// <summary>
    /// options to use <see cref="Mouse.MoveAsync(decimal, decimal, MoveOptions)"/>.
    /// </summary>
    internal class MoveOptions
    {
        /// <summary>
        /// Sends intermediate <c>mousemove</c> events. Defaults to 1.
        /// </summary>
        public int Steps { get; set; } = 1;
    }
}
