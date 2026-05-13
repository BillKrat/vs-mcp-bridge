namespace VsMcpBridge.Shared.Tools
{
    public sealed class RegexTextSearchMatch
    {
        public int EntryIndex { get; set; }
        public string EntryText { get; set; } = string.Empty;
        public int MatchIndex { get; set; }
        public int MatchLength { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
