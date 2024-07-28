using PuppeteerSharp;

namespace ZingPDF.FromHTML
{
    public static class Converter
    {
        public static async Task<Stream> ToPdfAsync(Uri uri, NavigationOptions? navigationOptions = null)
        {
            navigationOptions ??= NavigationOptions.Default;

            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            await using var page = await browser.NewPageAsync();

            await page.GoToAsync(
                uri.AbsoluteUri,
                timeout: navigationOptions.TimeoutExpiration,
                waitUntil: navigationOptions.WaitUntilFlags.ToWaitUntilNavigations()
                );

            // Wait for fonts to be loaded. Omitting this might result in no text rendered in pdf.
            await page.EvaluateExpressionHandleAsync("document.fonts.ready");
            await page.EmulateMediaTypeAsync(PuppeteerSharp.Media.MediaType.Screen);

            return await page.PdfStreamAsync();
        }
    }
}
