namespace ZingPDF.Parsing.IncrementalUpdates
{
    internal class IncrementalUpdateOptions
    {
        public bool RenderCrossReferencesAsStream { get; set; }

        public static IncrementalUpdateOptions Default { get; } = new();
    }
}
