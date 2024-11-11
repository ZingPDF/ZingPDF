using PuppeteerSharp;

namespace ZingPDF.FromHTML
{
    public static class Converter
    {
        public static async Task<Stream> ToPdfAsync(string htmlContent)
        {
            using (var browser = await PrepareBrowserAsync())
            using (var page = await PreparePageAsync(browser))
            {
                await page.SetContentAsync(htmlContent);

                return await page.PdfStreamAsync();
            }  
        }

        public static async Task<Stream> ToPdfAsync(Uri uri, NavigationOptions? navigationOptions = null)
        {
            navigationOptions ??= NavigationOptions.Default;

            using var browser = await PrepareBrowserAsync();
            using var page = await PreparePageAsync(browser);

            await page.GoToAsync(
                uri.AbsoluteUri,
                timeout: navigationOptions.TimeoutExpiration,
                waitUntil: navigationOptions.WaitUntilFlags.ToWaitUntilNavigations()
                );

            // Wait for fonts to be loaded. Omitting this might result in no text rendered in pdf.
            await page.EvaluateExpressionHandleAsync("document.fonts.ready");

            return await page.PdfStreamAsync();
        }

        private static async Task<IBrowser> PrepareBrowserAsync()
        {
            await new BrowserFetcher().DownloadAsync();

            return await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, DumpIO = true });
        }

        private static async Task<IPage> PreparePageAsync(IBrowser browser)
        {
            var page = await browser.NewPageAsync();

            await page.EmulateMediaTypeAsync(PuppeteerSharp.Media.MediaType.Screen);

            return page;
        } 
    }
}
