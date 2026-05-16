using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class Bm25TextSearchTool : IBridgeTool
    {
        public const string ToolId = "bridge.bm25TextSearch";
        private const double K1 = 1.2;
        private const double B = 0.75;
        private static readonly Regex TokenRegex = new Regex(@"[\p{L}\p{N}_]+", RegexOptions.CultureInvariant);

        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = ToolId,
            Name = "BM25 Text Search",
            Description = "Ranks supplied in-memory text documents with a minimal BM25-style scorer.",
            Category = "Search",
            Source = "Compiled",
            Host = "Shared"
        };

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetStringArgument(request, "query", out var query))
            {
                return Task.FromResult(BridgeToolResult.Failed(
                    request,
                    "InvalidRequest",
                    "BM25 text search requires a non-empty 'query' argument."));
            }

            var caseSensitive = TryGetBooleanArgument(request, "caseSensitive", out var parsedCaseSensitive)
                ? parsedCaseSensitive
                : false;
            var maxResults = TryGetIntegerArgument(request, "maxResults", out var parsedMaxResults)
                ? parsedMaxResults
                : int.MaxValue;

            if (maxResults <= 0)
            {
                return Task.FromResult(BridgeToolResult.Failed(
                    request,
                    "InvalidRequest",
                    "BM25 text search requires 'maxResults' to be greater than zero when provided."));
            }

            var queryTerms = Tokenize(query!, caseSensitive);
            if (queryTerms.Count == 0)
            {
                return Task.FromResult(BridgeToolResult.Failed(
                    request,
                    "InvalidRequest",
                    "BM25 text search requires a query containing at least one searchable token."));
            }

            var documents = GetDocuments(request);
            if (documents.Count == 0)
            {
                return Task.FromResult(BridgeToolResult.Failed(
                    request,
                    "InvalidRequest",
                    "BM25 text search requires a non-empty 'documents' or 'entries' collection."));
            }

            var analyzedDocuments = documents
                .Select((document, index) => new AnalyzedDocument(document, index, Tokenize(document.Text, caseSensitive)))
                .ToArray();
            var averageDocumentLength = analyzedDocuments.Length == 0
                ? 0
                : analyzedDocuments.Average(document => document.Length);
            var documentFrequencies = queryTerms
                .Distinct(StringComparer.Ordinal)
                .ToDictionary(
                    term => term,
                    term => analyzedDocuments.Count(document => document.TermCounts.ContainsKey(term)),
                    StringComparer.Ordinal);

            var ranked = new List<Bm25TextSearchResult>();
            foreach (var document in analyzedDocuments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var score = ScoreDocument(queryTerms, document, documentFrequencies, analyzedDocuments.Length, averageDocumentLength);
                if (score <= 0)
                    continue;

                ranked.Add(new Bm25TextSearchResult
                {
                    DocumentIndex = document.Index,
                    DocumentId = document.Document.Id,
                    Text = document.Document.Text,
                    Score = Math.Round(score, 6, MidpointRounding.AwayFromZero)
                });
            }

            var orderedResults = ranked
                .OrderByDescending(result => result.Score)
                .ThenBy(result => result.DocumentIndex)
                .ToArray();
            var returnedResults = orderedResults.Take(maxResults).ToArray();
            var data = new Dictionary<string, object?>
            {
                ["results"] = returnedResults,
                ["resultCount"] = returnedResults.Length,
                ["totalResultCount"] = orderedResults.Length,
                ["limited"] = orderedResults.Length > returnedResults.Length
            };

            return Task.FromResult(BridgeToolResult.Succeeded(
                request,
                $"BM25 text search completed with {returnedResults.Length} ranked result(s).",
                data));
        }

        private static double ScoreDocument(
            IReadOnlyList<string> queryTerms,
            AnalyzedDocument document,
            IReadOnlyDictionary<string, int> documentFrequencies,
            int documentCount,
            double averageDocumentLength)
        {
            if (document.Length == 0 || averageDocumentLength <= 0)
                return 0;

            var score = 0d;
            foreach (var term in queryTerms)
            {
                if (!document.TermCounts.TryGetValue(term, out var termFrequency))
                    continue;

                var documentFrequency = documentFrequencies[term];
                var idf = Math.Log(1 + (documentCount - documentFrequency + 0.5) / (documentFrequency + 0.5));
                var denominator = termFrequency + K1 * (1 - B + B * document.Length / averageDocumentLength);
                score += idf * (termFrequency * (K1 + 1) / denominator);
            }

            return score;
        }

        private static IReadOnlyList<string> Tokenize(string text, bool caseSensitive)
        {
            var tokens = new List<string>();
            foreach (Match match in TokenRegex.Matches(text))
            {
                tokens.Add(caseSensitive
                    ? match.Value
                    : match.Value.ToUpperInvariant());
            }

            return tokens;
        }

        private static bool TryGetStringArgument(BridgeToolRequest request, string key, out string? value)
        {
            value = null;
            if (!request.Arguments.TryGetValue(key, out var rawValue))
                return false;

            value = rawValue as string;
            return !string.IsNullOrWhiteSpace(value);
        }

        private static IReadOnlyList<Bm25TextSearchDocument> GetDocuments(BridgeToolRequest request)
        {
            if (request.Arguments.TryGetValue("documents", out var rawDocuments) && rawDocuments != null)
            {
                var documents = ConvertDocuments(rawDocuments);
                if (documents.Count > 0)
                    return documents;
            }

            if (request.Arguments.TryGetValue("entries", out var rawEntries) && rawEntries != null)
                return ConvertEntries(rawEntries);

            return Array.Empty<Bm25TextSearchDocument>();
        }

        private static IReadOnlyList<Bm25TextSearchDocument> ConvertDocuments(object rawDocuments)
        {
            if (rawDocuments is IEnumerable<Bm25TextSearchDocument> typedDocuments)
                return typedDocuments.Where(document => !string.IsNullOrWhiteSpace(document.Text)).ToArray();

            if (rawDocuments is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                return ConvertJsonDocuments(jsonElement);

            if (rawDocuments is IEnumerable documents && !(rawDocuments is string))
            {
                var values = new List<Bm25TextSearchDocument>();
                foreach (var document in documents)
                {
                    if (TryConvertDocument(document, values.Count, out var converted))
                        values.Add(converted);
                }

                return values;
            }

            return Array.Empty<Bm25TextSearchDocument>();
        }

        private static IReadOnlyList<Bm25TextSearchDocument> ConvertEntries(object rawEntries)
        {
            if (rawEntries is IEnumerable<string> stringEntries)
                return stringEntries
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select((entry, index) => new Bm25TextSearchDocument { Id = index.ToString(), Text = entry })
                    .ToArray();

            if (rawEntries is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                return jsonElement.EnumerateArray()
                    .Where(entry => entry.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(entry.GetString()))
                    .Select((entry, index) => new Bm25TextSearchDocument { Id = index.ToString(), Text = entry.GetString()! })
                    .ToArray();

            if (rawEntries is IEnumerable entries && !(rawEntries is string))
            {
                var values = new List<Bm25TextSearchDocument>();
                foreach (var entry in entries)
                {
                    if (entry is string text && !string.IsNullOrWhiteSpace(text))
                        values.Add(new Bm25TextSearchDocument { Id = values.Count.ToString(), Text = text });
                }

                return values;
            }

            return Array.Empty<Bm25TextSearchDocument>();
        }

        private static IReadOnlyList<Bm25TextSearchDocument> ConvertJsonDocuments(JsonElement documents)
        {
            var values = new List<Bm25TextSearchDocument>();
            foreach (var document in documents.EnumerateArray())
            {
                if (TryConvertDocument(document, values.Count, out var converted))
                    values.Add(converted);
            }

            return values;
        }

        private static bool TryConvertDocument(object? rawDocument, int fallbackIndex, out Bm25TextSearchDocument document)
        {
            document = new Bm25TextSearchDocument();
            if (rawDocument is Bm25TextSearchDocument typedDocument)
            {
                if (string.IsNullOrWhiteSpace(typedDocument.Text))
                    return false;

                document = typedDocument;
                return true;
            }

            if (rawDocument is string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                document = new Bm25TextSearchDocument { Id = fallbackIndex.ToString(), Text = text };
                return true;
            }

            if (rawDocument is IReadOnlyDictionary<string, object?> readOnlyDictionary)
                return TryConvertDictionaryDocument(readOnlyDictionary, fallbackIndex, out document);

            if (rawDocument is IDictionary<string, object?> dictionary)
                return TryConvertDictionaryDocument(dictionary, fallbackIndex, out document);

            if (rawDocument is JsonElement jsonElement)
                return TryConvertJsonDocument(jsonElement, fallbackIndex, out document);

            return false;
        }

        private static bool TryConvertDictionaryDocument(
            IEnumerable<KeyValuePair<string, object?>> values,
            int fallbackIndex,
            out Bm25TextSearchDocument document)
        {
            document = new Bm25TextSearchDocument();
            string? id = null;
            string? text = null;
            foreach (var pair in values)
            {
                if (string.Equals(pair.Key, "id", StringComparison.OrdinalIgnoreCase))
                    id = pair.Value?.ToString();
                else if (string.Equals(pair.Key, "text", StringComparison.OrdinalIgnoreCase))
                    text = pair.Value as string;
            }

            if (string.IsNullOrWhiteSpace(text))
                return false;

            document = new Bm25TextSearchDocument { Id = id ?? fallbackIndex.ToString(), Text = text! };
            return true;
        }

        private static bool TryConvertJsonDocument(JsonElement jsonElement, int fallbackIndex, out Bm25TextSearchDocument document)
        {
            document = new Bm25TextSearchDocument();
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                var text = jsonElement.GetString();
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                document = new Bm25TextSearchDocument { Id = fallbackIndex.ToString(), Text = text! };
                return true;
            }

            if (jsonElement.ValueKind != JsonValueKind.Object)
                return false;

            string? id = null;
            string? textValue = null;
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase))
                    id = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.ToString();
                else if (string.Equals(property.Name, "text", StringComparison.OrdinalIgnoreCase))
                    textValue = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            }

            if (string.IsNullOrWhiteSpace(textValue))
                return false;

            document = new Bm25TextSearchDocument { Id = id ?? fallbackIndex.ToString(), Text = textValue! };
            return true;
        }

        private static bool TryGetBooleanArgument(BridgeToolRequest request, string key, out bool value)
        {
            value = false;
            if (!request.Arguments.TryGetValue(key, out var rawValue) || rawValue == null)
                return false;

            if (rawValue is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            return bool.TryParse(rawValue.ToString(), out value);
        }

        private static bool TryGetIntegerArgument(BridgeToolRequest request, string key, out int value)
        {
            value = 0;
            if (!request.Arguments.TryGetValue(key, out var rawValue) || rawValue == null)
                return false;

            if (rawValue is int intValue)
            {
                value = intValue;
                return true;
            }

            return int.TryParse(rawValue.ToString(), out value);
        }

        private sealed class AnalyzedDocument
        {
            public AnalyzedDocument(Bm25TextSearchDocument document, int index, IReadOnlyList<string> tokens)
            {
                Document = document;
                Index = index;
                Length = tokens.Count;
                TermCounts = tokens
                    .GroupBy(token => token, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            }

            public Bm25TextSearchDocument Document { get; }
            public int Index { get; }
            public int Length { get; }
            public IReadOnlyDictionary<string, int> TermCounts { get; }
        }
    }
}
