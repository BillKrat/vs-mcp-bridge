namespace VsMcpBridge.Shared.Tools
{
    public sealed class Bm25TextSearchResult
    {
        public int DocumentIndex { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
