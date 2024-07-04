using ZingPDF.Drawing;
using ZingPDF.Forms;
using ZingPDF.ObjectModel.CommonDataStructures;
using ZingPDF.ObjectModel.DocumentStructure.PageTree;

namespace ZingPDF;

public interface IEditablePdf : IPdf
{
    Task AppendPageAsync(Page.PageCreationOptions? pageCreationOptions);
    Task InsertPageAsync(int pageNumber, Page.PageCreationOptions? pageCreationOptions);
    Task DeletePageAsync(int pageNumber);
    Task SetPageRotationAsync(int pageNumber, Rotation rotation);
    void Draw(
        int pageNumber,
        IEnumerable<Drawing.Path> paths,
        IEnumerable<Text> text,
        IEnumerable<Image> imageOperations,
        CoordinateSystem coordinateSystem = CoordinateSystem.BottomUp
        );

    Task CompleteFormAsync(IDictionary<string, string> formValues);
    Task<IEnumerable<FormField>> GetFieldsAsync();

    void AddWatermark();
    void Compress(int dpi, int quality);
    void Encrypt();
    void Decrypt();
    void Sign();
    void AppendPdf(Stream stream);
}
