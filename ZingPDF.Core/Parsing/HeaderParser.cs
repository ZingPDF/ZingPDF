using MorseCode.ITask;
using System.Text;
using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing
{
    internal class HeaderParser : IPdfObjectParser<Header>
    {
        public async ITask<Header> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync("%PDF-");

            var version = Encoding.ASCII.GetString(new []
            {
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
            });

            return new Header(double.Parse(version));
        }
    }
}
