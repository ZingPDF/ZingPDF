using MorseCode.ITask;
using ZingPDF.Extensions;
using ZingPDF.Syntax.FileStructure.Trailer;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;

namespace ZingPDF.Parsing.Parsers.FileStructure
{
    internal class TrailerParser : IParser<Trailer>
    {
        private readonly IParser<Dictionary> _dictionaryParser;
        private readonly IParser<Keyword> _keywordParser;
        private readonly IParser<Number> _numberParser;

        public TrailerParser(
            IParser<Dictionary> dictionaryParser,
            IParser<Keyword> keywordParser,
            IParser<Number> numberParser
            )
        {
            _dictionaryParser = dictionaryParser;
            _keywordParser = keywordParser;
            _numberParser = numberParser;
        }

        public async ITask<Trailer> ParseAsync(Stream stream, ParseContext context)
        {
            await stream.AdvanceBeyondNextAsync(Constants.Trailer);

            var trailerDict = TrailerDictionary.FromDictionary(await _dictionaryParser.ParseAsync(stream, context));

            _ = await _keywordParser.ParseAsync(stream, context); // startxref
            var xrefTableOffset = await _numberParser.ParseAsync(stream, context);

            return new Trailer(trailerDict, xrefTableOffset, context.Origin);
        }
    }
}
