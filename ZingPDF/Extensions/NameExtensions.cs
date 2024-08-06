using ZingPDF.Elements.Forms;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Extensions
{
    internal static class NameExtensions
    {
        public static FormFieldType ToFormFieldType(this Name name)
        {
            return (string)name switch
            {
                "Btn" => FormFieldType.Button,
                "Tx" => FormFieldType.Text,
                "Ch" => FormFieldType.Choice,
                "Sig" => FormFieldType.Signature,
                _ => throw new NotSupportedException("Unsupported field type encountered"),
            };
        }
    }
}
