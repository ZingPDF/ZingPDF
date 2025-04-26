using FakeItEasy;
using FluentAssertions;
using Xunit;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.Graphics.Images;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Linearization;
using ZingPDF.Syntax;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.DocumentStructure.PageTree;
using ZingPDF.Syntax.FileStructure.CrossReferences.CrossReferenceStreams;
using ZingPDF.Syntax.FileStructure.ObjectStreams;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Text;

namespace ZingPDF.Parsing.Parsers.Objects.Dictionaries;

public class DictionaryIdentifierTests
{
    [Theory]
    [InlineData(Constants.DictionaryTypes.Catalog, typeof(DocumentCatalogDictionary))]
    [InlineData(Constants.DictionaryTypes.Page, typeof(PageDictionary))]
    [InlineData(Constants.DictionaryTypes.Pages, typeof(PageTreeNodeDictionary))]
    [InlineData(Constants.DictionaryTypes.XRef, typeof(CrossReferenceStreamDictionary))]
    [InlineData(Constants.DictionaryTypes.ObjStm, typeof(ObjectStreamDictionary))]
    [InlineData(Constants.DictionaryTypes.XObject, typeof(XObjectDictionary))]
    [InlineData(Constants.DictionaryTypes.Font, typeof(FontDictionary))]
    [InlineData(Constants.DictionaryTypes.FontDescriptor, typeof(FontDescriptorDictionary))]
    [InlineData(Constants.DictionaryTypes.Annot, typeof(AnnotationDictionary))]
    public async Task IdentifyByType(string typeName, Type expectedType)
    {
        (await DictionaryIdentifier.IdentifyAsync(
            new Dictionary<Name, IPdfObject>
            {
                [Constants.DictionaryKeys.Type] = (Name)typeName
            },
            EmptyPdfEditor.Instance
        ))
        .Should().Be(expectedType);
    }

    [Theory]
    [InlineData(Constants.DictionaryTypes.XObject, XObjectDictionary.Subtypes.Form, typeof(Type1FormDictionary))]
    [InlineData(Constants.DictionaryTypes.XObject, XObjectDictionary.Subtypes.Image, typeof(ImageDictionary))]
    [InlineData(Constants.DictionaryTypes.Annot, AnnotationDictionary.Subtypes.Widget, typeof(WidgetAnnotationDictionary))]
    public async Task IdentifyBySubtype(string typeName, string subtypeName, Type expectedType)
    {
        (await DictionaryIdentifier.IdentifyAsync(
            new Dictionary<Name, IPdfObject>
            {
                [Constants.DictionaryKeys.Type] = (Name)typeName,
                [Constants.DictionaryKeys.Subtype] = (Name)subtypeName, 
            },
            EmptyPdfEditor.Instance
            ))
            .Should().Be(expectedType);
    }

    [Theory]
    [InlineData("Btn")]
    [InlineData("Tx")]
    [InlineData("Ch")]
    [InlineData("Sig")]
    public async Task IdentifyFieldDictionary(string fieldType)
    {
        var dictionary = new Dictionary<Name, IPdfObject>
        {
            [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Annot,
            [Constants.DictionaryKeys.Subtype] = (Name)AnnotationDictionary.Subtypes.Widget,
            [Constants.DictionaryKeys.Field.FT] = (Name)fieldType,
        };
        (await DictionaryIdentifier.IdentifyAsync(dictionary, EmptyPdfEditor.Instance))
            .Should().Be(typeof(FieldDictionary));
    }

    [Fact]
    public async Task IdentifyFieldDictionaryByInheritance()
    {
        const int parentIndex = 572;
        const int parentGenerationNumber = 0;

        var pdfEditor = A.Fake<IPdfEditor>();
        A.CallTo(() => pdfEditor.GetAsync<Dictionary>(new IndirectObjectReference(parentIndex, parentGenerationNumber)))
            .Returns(new Dictionary(new Dictionary<Name, IPdfObject> { [Constants.DictionaryKeys.Field.FT] = new Name("Btn") }, pdfEditor));

        var dictionary = new Dictionary<Name, IPdfObject>
        {
            [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Annot,
            [Constants.DictionaryKeys.Subtype] = (Name)AnnotationDictionary.Subtypes.Widget,
            [Constants.DictionaryKeys.Parent] = new IndirectObjectReference(parentIndex, 0),
        };
        (await DictionaryIdentifier.IdentifyAsync(dictionary, pdfEditor))
            .Should().Be(typeof(FieldDictionary));
    }

    [Fact]
    public async Task IdentifyStreamDictionary()
    {
        var dictionary = new Dictionary<Name, IPdfObject>
        {
            [Constants.DictionaryKeys.Stream.Length] = (Number)1,
        };

        (await DictionaryIdentifier.IdentifyAsync(dictionary, EmptyPdfEditor.Instance))
            .Should().Be(typeof(StreamDictionary));
    }

    [Fact]
    public async Task IdentifyLinearizationDictionary()
    {
        var dictionary = new Dictionary<Name, IPdfObject>
        {
            [Constants.DictionaryKeys.LinearizationParameter.Linearized] = (Number)1,
        };

        (await DictionaryIdentifier.IdentifyAsync(dictionary, EmptyPdfEditor.Instance))
            .Should().Be(typeof(LinearizationParameterDictionary));
    }

    [Fact]
    public async Task IdentifyInteractiveDictionary()
    {
        var dictionary = new Dictionary<Name, IPdfObject>
        {
            [Constants.DictionaryKeys.InteractiveForm.Fields] = (ArrayObject)[],
        };

        (await DictionaryIdentifier.IdentifyAsync(dictionary, EmptyPdfEditor.Instance))
            .Should().Be(typeof(InteractiveFormDictionary));
    }

    [Fact]
    public async Task IdentifyAppearanceDictionary()
    {
        var dictionary = new Dictionary<Name, IPdfObject>
        {
            [Constants.DictionaryKeys.Appearance.N] = new Dictionary([], EmptyPdfEditor.Instance),
        };

        (await DictionaryIdentifier.IdentifyAsync(dictionary, EmptyPdfEditor.Instance))
            .Should().Be(typeof(AppearanceDictionary));
    }

    [Fact]
    public async Task IdentifyFontDictionaryWithSubtype()
    {
        var dictionary = new Dictionary<Name, IPdfObject>
        {
            [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Font,
            [Constants.DictionaryKeys.Subtype] = (Name)FontDictionary.Subtypes.Type1,
        };
        (await DictionaryIdentifier.IdentifyAsync(dictionary, EmptyPdfEditor.Instance))
            .Should().Be(typeof(FontDictionary), because: "Classes for font subtypes are not implemented");
    }
}
