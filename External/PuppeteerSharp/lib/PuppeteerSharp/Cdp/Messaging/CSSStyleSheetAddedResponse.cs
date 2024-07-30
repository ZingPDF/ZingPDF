namespace PuppeteerSharp.Cdp.Messaging
{
    internal class CSSStyleSheetAddedResponse
    {
        public CSSStyleSheetAddedResponseHeader Header { get; set; }

        internal class CSSStyleSheetAddedResponseHeader
        {
            public string StyleSheetId { get; set; }

            public string SourceURL { get; set; }
        }
    }
}
