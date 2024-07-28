using PuppeteerSharp;

namespace ZingPDF.FromHTML
{
    internal static class WaitUntilExtensions
    {
        public static WaitUntilNavigation[] ToWaitUntilNavigations(this WaitUntil? waitUntil)
        {
            if (waitUntil == null)
            {
                return [];
            }

            var navigationList = new List<WaitUntilNavigation>();

            if (waitUntil.Value.HasFlag(WaitUntil.Load))
            {
                navigationList.Add(WaitUntilNavigation.Load);
            }
            if (waitUntil.Value.HasFlag(WaitUntil.DOMContentLoaded))
            {
                navigationList.Add(WaitUntilNavigation.DOMContentLoaded);
            }
            if (waitUntil.Value.HasFlag(WaitUntil.Networkidle0))
            {
                navigationList.Add(WaitUntilNavigation.Networkidle0);
            }
            if (waitUntil.Value.HasFlag(WaitUntil.Networkidle2))
            {
                navigationList.Add(WaitUntilNavigation.Networkidle2);
            }

            return [.. navigationList];
        }
    }
}
