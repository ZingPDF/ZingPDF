//using FakeItEasy;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;
//using ZingPDF.Parsing.Parsers;
//using ZingPDF.Syntax;

//namespace ZingPDF.Tests.Common;

//using System.Reflection;
//using FakeItEasy;
//using System.Collections.Generic;

//public class FakeContextBuilder
//{
//    private readonly Dictionary<Type, object> _parsers = [];

//    public FakeContextBuilder WithParser<T>(IParser<T> parser) where T : IPdfObject
//    {
//        _parsers[typeof(T)] = parser;
//        return this;
//    }

//    public IPdfContext Build()
//    {
//        var registry = A.Fake<IParserRegistry>();

//        // Setup GetParserFor<T>()
//        foreach (var kvp in _parsers)
//        {
//            var parserType = typeof(IParser<>).MakeGenericType(kvp.Key);
//            var configureMethod = typeof(FakeContextBuilder)
//                .GetMethod(nameof(ConfigureGenericParser), BindingFlags.NonPublic | BindingFlags.Static)!
//                .MakeGenericMethod(kvp.Key);

//            configureMethod.Invoke(null, [registry, kvp.Value]);
//        }

//        // Setup GetParserFor(Type)
//        A.CallTo(() => registry.GetParserFor(A<Type>.Ignored))
//            .ReturnsLazily((call) =>
//            {
//                var type = (Type)call.Arguments[0];
//                if (_parsers.TryGetValue(type, out var parser))
//                    return (IParser<IPdfObject>)parser;

//                throw new InvalidOperationException($"No parser registered for type {type.Name}");
//            });

//        var context = A.Fake<IPdfContext>();
//        A.CallTo((object)(() => context.Parsers)).Returns(registry);

//        return context;
//    }

//    private static void ConfigureGenericParser<T>(IParserRegistry registry, object parser) where T : IPdfObject
//    {
//        A.CallTo(() => registry.GetParserFor<T>()).Returns((IParser<T>)parser);
//    }
//}
