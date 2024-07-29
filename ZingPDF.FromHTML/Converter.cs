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

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, DumpIO = true });
            var page = await browser.NewPageAsync();

            await page.EmulateMediaTypeAsync(PuppeteerSharp.Media.MediaType.Screen);

            await page.GoToAsync(
                uri.AbsoluteUri,
                timeout: navigationOptions.TimeoutExpiration,
                waitUntil: navigationOptions.WaitUntilFlags.ToWaitUntilNavigations()
                );

            // Wait for fonts to be loaded. Omitting this might result in no text rendered in pdf.
            await page.EvaluateExpressionHandleAsync("document.fonts.ready");

            var pdfStream = await page.PdfStreamAsync();

            try
            {
                await page.CloseAsync();
                await browser.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return pdfStream;
        }
    }
}
