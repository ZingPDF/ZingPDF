using System.Runtime.CompilerServices;
using System.Text;
using ZingPDF.Text.Encoding.PDFDocEncoding;

namespace ZingPDF;

internal static class EncodingBootstrapper
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.RegisterProvider(PDFDocEncodingProvider.Instance);
    }
}
