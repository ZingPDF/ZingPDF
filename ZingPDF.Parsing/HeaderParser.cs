using MorseCode.ITask;
using System.Text;
using ZingPDF.Extensions;
using ZingPDF.Objects;

namespace ZingPDF.Parsing
{
    internal class HeaderParser : IPdfObjectParser<Header>
    {
        public async ITask<Header> ParseAsync(Stream stream)
        {
            await stream.AdvanceBeyondNextAsync("%PDF-");

            var version = Encoding.ASCII.GetString(new[]
            {
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
            });

            return new Header(double.Parse(version));
        }
    }
}
