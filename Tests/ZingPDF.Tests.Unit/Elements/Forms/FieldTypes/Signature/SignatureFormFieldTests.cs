using FakeItEasy;
using FluentAssertions;
using ZingPDF.Elements.Forms;
using ZingPDF.Elements.Forms.FieldTypes.Signature;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;
using Xunit;

namespace ZingPDF.Tests.Unit.Elements.Forms.FieldTypes.Signature;

public class SignatureFormFieldTests
{
    [Fact]
    public async Task HasSignatureValueAsync_ReturnsFalse_WhenFieldIsUnsigned()
    {
        var pdf = A.Fake<IPdf>();
        var fieldDictionary = new FieldDictionary(new Dictionary([], pdf, ObjectContext.UserCreated));
        var field = CreateField(pdf, fieldDictionary);

        (await field.HasSignatureValueAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task SignatureMetadataMethods_ReturnExpectedValues_WhenSignatureDictionaryExists()
    {
        var pdf = A.Fake<IPdf>();
        var signingDate = new DateTimeOffset(2026, 3, 28, 10, 15, 0, TimeSpan.FromHours(11));
        var signatureDictionary = new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>("Filter", new Name("Adobe.PPKLite", ObjectContext.UserCreated)),
                new KeyValuePair<string, IPdfObject>("SubFilter", new Name("adbe.pkcs7.detached", ObjectContext.UserCreated)),
                new KeyValuePair<string, IPdfObject>("Name", PdfString.FromAscii("Taylor Smith", PdfStringSyntax.Literal, ObjectContext.UserCreated)),
                new KeyValuePair<string, IPdfObject>("Reason", PdfString.FromAscii("Approved for release", PdfStringSyntax.Literal, ObjectContext.UserCreated)),
                new KeyValuePair<string, IPdfObject>("M", new Date(signingDate, ObjectContext.UserCreated))
            ],
            pdf,
            ObjectContext.UserCreated);
        var fieldDictionary = new FieldDictionary(new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>(Constants.DictionaryKeys.Field.V, signatureDictionary)
            ],
            pdf,
            ObjectContext.UserCreated));
        var field = CreateField(pdf, fieldDictionary);

        (await field.HasSignatureValueAsync()).Should().BeTrue();
        (await field.GetFilterAsync()).Should().Be("Adobe.PPKLite");
        (await field.GetSubFilterAsync()).Should().Be("adbe.pkcs7.detached");
        (await field.GetSignerNameAsync()).Should().Be("Taylor Smith");
        (await field.GetReasonAsync()).Should().Be("Approved for release");
        (await field.GetSigningDateAsync()).Should().Be(signingDate);
    }

    private static SignatureFormField CreateField(IPdf pdf, FieldDictionary fieldDictionary)
    {
        var parentContainer = new Dictionary([], pdf, ObjectContext.UserCreated);
        var acroForm = new OptionalProperty<InteractiveFormDictionary>("AcroForm", parentContainer, pdf);
        var form = new Form(acroForm, pdf, A.Fake<IParser<ContentStream>>());
        var fieldObject = new IndirectObject(new IndirectObjectId(1, 0), fieldDictionary);

        return new SignatureFormField(
            fieldObject,
            "ApprovalSignature",
            null,
            new FieldProperties(0),
            form,
            pdf);
    }
}
