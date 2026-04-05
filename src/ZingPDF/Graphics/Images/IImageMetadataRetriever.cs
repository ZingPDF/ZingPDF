namespace ZingPDF.Graphics.Images
{
    internal interface IImageMetadataRetriever
    {
        Task<ImageMetadata> GetAsync(Stream image);
    }
}
