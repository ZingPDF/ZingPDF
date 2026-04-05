using ZingPDF;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Text;

Directory.CreateDirectory("output");

await using var input = File.OpenRead(Path.Combine("testfiles", "pdf", "complex-form.pdf"));
await using var output = File.Create(Path.Combine("output", "filled-and-flattened-form.pdf"));
using var pdf = Pdf.Load(input);

var form = await pdf.GetFormAsync();
if (form is null)
{
    return;
}

var textField = (await form.GetFieldsAsync()).OfType<TextFormField>().FirstOrDefault();
if (textField is not null)
{
    await textField.SetValueAsync("Ada Lovelace");
}

var choiceField = (await form.GetFieldsAsync()).OfType<ChoiceFormField>().FirstOrDefault();
if (choiceField is not null)
{
    var options = await choiceField.GetOptionsAsync();
    var option = options.FirstOrDefault();
    if (option is not null)
    {
        await option.SelectAsync();
    }
}

var checkbox = (await form.GetFieldsAsync()).OfType<CheckboxFormField>().FirstOrDefault();
if (checkbox is not null)
{
    var options = await checkbox.GetOptionsAsync();
    var option = options.FirstOrDefault();
    if (option is not null)
    {
        await option.SelectAsync();
    }
}

await form.FlattenAsync();
await pdf.SaveAsync(output);
