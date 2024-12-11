using FluentAssertions;
using Xunit;
using ZingPDF.UnitTests.TestFiles;

namespace ZingPDF.Linearization;

public class LinearizationParserTests
{
    [Fact]
     public async Task GetLinearizationDictionary_FindsDictionary_WhenPresent()
    {
        var dict = await new LinearizationParser()
            .GetLinearizationDictionaryAsync(Files.AsStream(Files.MikeyPortfolio));

        dict.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLinearizationDictionary_Null_WhenNotPresent()
    {
        var dict = await new LinearizationParser()
            .GetLinearizationDictionaryAsync(Files.AsStream(Files.Minimal1));

        dict.Should().BeNull();
    }

    [Fact]
    public async Task GetLinearizationDictionary_CorrectStreamPosition()
    {
        var stream = Files.AsStream(Files.MikeyPortfolio);

        var dict = await new LinearizationParser()
            .GetLinearizationDictionaryAsync(stream);

        stream.Position.Should().Be(102, because: "After the linearization dictionary, parsing needs to continue from the end of the dictionary");
    }

    [Fact]
    public async Task IsLinearized_False_WhenModified()
    {
        var linearizationParser = new LinearizationParser();

        var stream = Files.AsStream(Files.Form);

        var dict = await linearizationParser
            .GetLinearizationDictionaryAsync(stream);

        linearizationParser.IsLinearized(dict!, stream).Should().BeFalse();
    }

    [Fact]
    public async Task IsLinearized_True_WhenUnmodified()
    {
        var linearizationParser = new LinearizationParser();

        var stream = Files.AsStream(Files.MikeyPortfolio);

        var dict = await linearizationParser
            .GetLinearizationDictionaryAsync(stream);

        linearizationParser.IsLinearized(dict!, stream).Should().BeTrue();
    }

    [Theory]
    [InlineData(Constants.DictionaryKeys.LinearizationParameter.Linearized)]
    [InlineData(Constants.DictionaryKeys.LinearizationParameter.L)]
    [InlineData(Constants.DictionaryKeys.LinearizationParameter.H)]
    [InlineData(Constants.DictionaryKeys.LinearizationParameter.O)]
    [InlineData(Constants.DictionaryKeys.LinearizationParameter.E)]
    [InlineData(Constants.DictionaryKeys.LinearizationParameter.N)]
    [InlineData(Constants.DictionaryKeys.LinearizationParameter.T)]
    public async Task GetLinearizationDictionary_ContainsProperties(string key)
    {
        var dict = await new LinearizationParser()
            .GetLinearizationDictionaryAsync(Files.AsStream(Files.MikeyPortfolio));

        dict.Should().ContainKey(key);
    }
}
