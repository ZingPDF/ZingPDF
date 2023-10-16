using ZingPdf.Core.Extensions;

namespace ZingPdf.Core.Objects.DataStructures
{
    internal class Date : PdfObject
    {
        public Date(DateTime dateTime)
        {
            DateTime = dateTime;
        }

        public DateTime DateTime { get; }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteTextAsync(DateTime.ToString("D:yyyyMMddHHmmsszz'00'"));
        }
    }
}
