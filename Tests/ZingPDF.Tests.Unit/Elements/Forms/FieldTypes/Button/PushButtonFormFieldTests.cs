using FakeItEasy;
using FluentAssertions;
using ZingPDF.Elements.Forms;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;
using Xunit;

namespace ZingPDF.Tests.Unit.Elements.Forms.FieldTypes.Button;

public class PushButtonFormFieldTests
{
    [Fact]
    public async Task GetCaptionAsync_ReturnsCaptionFromAppearanceCharacteristics()
    {
        var pdf = A.Fake<IPdf>();
        var fieldDictionary = new FieldDictionary(new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>(
                    Constants.DictionaryKeys.WidgetAnnotation.MK,
                    new Dictionary(
                        [
                            new KeyValuePair<string, IPdfObject>(
                                "CA",
                                PdfString.FromAscii("Submit", PdfStringSyntax.Literal, ObjectContext.UserCreated))
                        ],
                        pdf,
                        ObjectContext.UserCreated))
            ],
            pdf,
            ObjectContext.UserCreated));

        var field = CreateField(pdf, fieldDictionary, []);

        (await field.GetCaptionAsync()).Should().Be("Submit");
    }

    [Fact]
    public async Task HasActionAsync_ReturnsTrue_WhenWidgetDefinesAction()
    {
        var pdf = A.Fake<IPdf>();
        var fieldDictionary = new FieldDictionary(new Dictionary([], pdf, ObjectContext.UserCreated));
        var widgetDictionary = new WidgetAnnotationDictionary(new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>(
                    Constants.DictionaryKeys.WidgetAnnotation.A,
                    new Dictionary([], pdf, ObjectContext.UserCreated))
            ],
            pdf,
            ObjectContext.UserCreated));
        var widget = new IndirectObject(new IndirectObjectId(2, 0), widgetDictionary);

        var field = CreateField(pdf, fieldDictionary, [widget]);

        (await field.HasActionAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task HasActionAsync_ReturnsFalse_WhenNoActionsExist()
    {
        var pdf = A.Fake<IPdf>();
        var fieldDictionary = new FieldDictionary(new Dictionary([], pdf, ObjectContext.UserCreated));

        var field = CreateField(pdf, fieldDictionary, []);

        (await field.HasActionAsync()).Should().BeFalse();
    }

    private static PushButtonFormField CreateField(IPdf pdf, FieldDictionary fieldDictionary, IReadOnlyList<IndirectObject> kids)
    {
        var parentContainer = new Dictionary([], pdf, ObjectContext.UserCreated);
        var acroForm = new OptionalProperty<InteractiveFormDictionary>("AcroForm", parentContainer, pdf);
        var form = new Form(acroForm, pdf, A.Fake<IParser<ContentStream>>());
        var fieldObject = new IndirectObject(new IndirectObjectId(1, 0), fieldDictionary);

        return new PushButtonFormField(
            fieldObject,
            "SubmitButton",
            null,
            new FieldProperties(0),
            form,
            pdf,
            kids);
    }
}
