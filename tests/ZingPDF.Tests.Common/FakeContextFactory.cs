//using FakeItEasy;
//using ZingPDF.Parsing.Parsers;
//using ZingPDF.Syntax;

//namespace ZingPDF.Tests.Common;

//public static class FakeContextFactory
//{
//    public static IPdfContext WithParser<T>(IParser<T> parser) where T : IPdfObject
//    {
//        var registry = A.Fake<IParserRegistry>();
//        A.CallTo(() => registry.GetParserFor<T>()).Returns(parser);

//        var context = A.Fake<IPdfContext>();
//        A.CallTo((object)(() => context.Parsers)).Returns(registry);

//        return context;
//    }
//}
