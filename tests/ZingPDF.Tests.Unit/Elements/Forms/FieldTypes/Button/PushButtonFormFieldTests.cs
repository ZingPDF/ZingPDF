using FakeItEasy;
using FluentAssertions;
using ZingPDF.Elements.Forms;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
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
        var widgetDictionary = CreateWidgetDictionary(pdf, primaryAction: new Dictionary([], pdf, ObjectContext.UserCreated));
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

    [Fact]
    public async Task GetActionTypeAsync_ReturnsActionSubtype_FromPrimaryActionDictionary()
    {
        var pdf = A.Fake<IPdf>();
        var action = CreateActionDictionary(pdf, "URI");
        var fieldDictionary = new FieldDictionary(new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>(Constants.DictionaryKeys.WidgetAnnotation.A, action)
            ],
            pdf,
            ObjectContext.UserCreated));

        var field = CreateField(pdf, fieldDictionary, []);

        (await field.GetActionTypeAsync()).Should().Be("URI");
    }

    [Fact]
    public async Task GetActionUriAsync_ReturnsUri_FromWidgetActionDictionary()
    {
        var pdf = A.Fake<IPdf>();
        var fieldDictionary = new FieldDictionary(new Dictionary([], pdf, ObjectContext.UserCreated));
        var action = new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>("S", new Name("URI", ObjectContext.UserCreated)),
                new KeyValuePair<string, IPdfObject>("URI", PdfString.FromAscii("https://example.com/forms", PdfStringSyntax.Literal, ObjectContext.UserCreated))
            ],
            pdf,
            ObjectContext.UserCreated);
        var widget = new IndirectObject(
            new IndirectObjectId(2, 0),
            CreateWidgetDictionary(pdf, primaryAction: action));

        var field = CreateField(pdf, fieldDictionary, [widget]);

        (await field.GetActionUriAsync()).Should().Be("https://example.com/forms");
        (await field.GetNamedActionAsync()).Should().BeNull();
    }

    [Fact]
    public async Task GetNamedActionAsync_ReturnsName_WhenPrimaryActionIsNamed()
    {
        var pdf = A.Fake<IPdf>();
        var action = new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>("S", new Name("Named", ObjectContext.UserCreated)),
                new KeyValuePair<string, IPdfObject>("N", new Name("Print", ObjectContext.UserCreated))
            ],
            pdf,
            ObjectContext.UserCreated);
        var fieldDictionary = new FieldDictionary(new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>(Constants.DictionaryKeys.WidgetAnnotation.A, action)
            ],
            pdf,
            ObjectContext.UserCreated));

        var field = CreateField(pdf, fieldDictionary, []);

        (await field.GetNamedActionAsync()).Should().Be("Print");
        (await field.GetActionUriAsync()).Should().BeNull();
    }

    [Fact]
    public async Task GetAdditionalActionTriggersAsync_ReturnsDistinctSortedTriggerKeys()
    {
        var pdf = A.Fake<IPdf>();
        var fieldDictionary = new FieldDictionary(new Dictionary(
            [
                new KeyValuePair<string, IPdfObject>(
                    Constants.DictionaryKeys.WidgetAnnotation.AA,
                    new Dictionary(
                        [
                            new KeyValuePair<string, IPdfObject>("D", new Dictionary([], pdf, ObjectContext.UserCreated)),
                            new KeyValuePair<string, IPdfObject>("E", new Dictionary([], pdf, ObjectContext.UserCreated))
                        ],
                        pdf,
                        ObjectContext.UserCreated))
            ],
            pdf,
            ObjectContext.UserCreated));
        var widget = new IndirectObject(
            new IndirectObjectId(2, 0),
            CreateWidgetDictionary(
                pdf,
                additionalActions: new Dictionary(
                    [
                        new KeyValuePair<string, IPdfObject>("U", new Dictionary([], pdf, ObjectContext.UserCreated)),
                        new KeyValuePair<string, IPdfObject>("D", new Dictionary([], pdf, ObjectContext.UserCreated))
                    ],
                    pdf,
                    ObjectContext.UserCreated)));

        var field = CreateField(pdf, fieldDictionary, [widget]);

        (await field.GetAdditionalActionTriggersAsync()).Should().Equal("D", "E", "U");
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

    private static WidgetAnnotationDictionary CreateWidgetDictionary(IPdf pdf, Dictionary? primaryAction = null, Dictionary? additionalActions = null)
        => new(new Dictionary(
            BuildWidgetEntries(pdf, primaryAction, additionalActions),
            pdf,
            ObjectContext.UserCreated));

    private static IEnumerable<KeyValuePair<string, IPdfObject>> BuildWidgetEntries(IPdf pdf, Dictionary? primaryAction, Dictionary? additionalActions)
    {
        yield return new KeyValuePair<string, IPdfObject>(Constants.DictionaryKeys.Type, new Name(Constants.DictionaryTypes.Annot, ObjectContext.UserCreated));
        yield return new KeyValuePair<string, IPdfObject>(Constants.DictionaryKeys.Subtype, new Name(WidgetAnnotationDictionary.Subtypes.Widget, ObjectContext.UserCreated));

        if (primaryAction is not null)
        {
            yield return new KeyValuePair<string, IPdfObject>(Constants.DictionaryKeys.WidgetAnnotation.A, primaryAction);
        }

        if (additionalActions is not null)
        {
            yield return new KeyValuePair<string, IPdfObject>(Constants.DictionaryKeys.WidgetAnnotation.AA, additionalActions);
        }
    }

    private static Dictionary CreateActionDictionary(IPdf pdf, string actionType)
        => new(
            [
                new KeyValuePair<string, IPdfObject>("S", new Name(actionType, ObjectContext.UserCreated))
            ],
            pdf,
            ObjectContext.UserCreated);
}
