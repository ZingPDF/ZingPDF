using ZingPDF.Elements.Drawing;

namespace ZingPDF.Elements.Forms
{
    public interface IFormField
    {
        string? Description { get; }
        string Name { get; }
        FieldProperties Properties { get; }
        Task<Size> GetFieldDimensionsAsync();
    }
}