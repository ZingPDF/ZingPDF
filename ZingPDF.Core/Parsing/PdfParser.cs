using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing
{
    public class PdfParser
    {
        public async Task ParseAsync(Stream stream)
        {
            using var tokenReader = new TokenReverseStreamReader(stream);

            var trailerTokens = new List<string>();
            string? token;

            do
            {
                token = await tokenReader.NextAsync();

                if (token == null)
                {
                    throw new ParserException();
                }

                trailerTokens.Add(token);
            }
            while (token != Constants.Trailer);

            // Reading from end of file, so we need to reverse the list
            trailerTokens.Reverse();

            var trailer = Parser.For<Trailer>().Parse(trailerTokens);
        }
    }
}
