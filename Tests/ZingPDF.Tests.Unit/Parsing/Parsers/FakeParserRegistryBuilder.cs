using FakeItEasy;
using MorseCode.ITask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZingPDF.Parsing;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;

namespace ZingPDF.Tests.Unit.Parsing.Parsers;

public class FakeParserRegistryBuilder
{
    private readonly Dictionary<Type, IPdfObject> _returns = [];
    private readonly IParserResolver _resolver = A.Fake<IParserResolver>();

    public FakeParserRegistryBuilder AddParser<T>(T returnValue, int bytesToConsume = 0) where T : class, IPdfObject
    {
        var parser = A.Fake<IParser<T>>();
        A.CallTo(() => parser.ParseAsync(A<Stream>._, A<ParseContext>._))
            .Invokes(async (Stream s, ParseContext _) =>
            {
                var buffer = new byte[bytesToConsume];
                await s.ReadAsync(buffer);
            })
            .Returns(Task.FromResult(returnValue).AsITask());

        A.CallTo(() => _resolver.GetParser<T>()).Returns(parser);
        A.CallTo(() => _resolver.GetParserFor(typeof(T))).Returns(parser);

        _returns[typeof(T)] = returnValue;
        return this;
    }

    public IParserResolver Build() => _resolver;

    public IPdfObject GetReturnValue(Type type) => _returns[type];
}
