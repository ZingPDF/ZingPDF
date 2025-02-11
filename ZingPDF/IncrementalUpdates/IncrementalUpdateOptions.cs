namespace ZingPDF.IncrementalUpdates
{
    public class IncrementalUpdateOptions
    {
        public bool RenderCrossReferencesAsStream { get; set; }

        public static IncrementalUpdateOptions Default { get; } = new();
    }
}
