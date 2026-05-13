using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VsMcpBridge.Shared.Tools
{
    public sealed class RegexTextSearchTool : IBridgeTool
    {
        public const string ToolId = "bridge.regexTextSearch";

        public BridgeToolDescriptor Descriptor { get; } = new BridgeToolDescriptor
        {
            Id = ToolId,
            Name = "Regex Text Search",
            Description = "Searches supplied text entries with a regular expression.",
            Category = "Search",
            Source = "Compiled",
            Host = "Shared"
        };

        public Task<BridgeToolResult> ExecuteAsync(BridgeToolRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetStringArgument(request, "pattern", out var pattern)
                && !TryGetStringArgument(request, "query", out pattern))
            {
                return Task.FromResult(BridgeToolResult.Failed(
                    request,
                    "InvalidRequest",
                    "Regex text search requires a non-empty 'pattern' argument."));
            }

            var entries = GetEntries(request);
            if (entries.Count == 0)
            {
                return Task.FromResult(BridgeToolResult.Failed(
                    request,
                    "InvalidRequest",
                    "Regex text search requires 'inputText' or a non-empty 'entries' collection."));
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
                    "Regex text search requires 'maxResults' to be greater than zero when provided."));
            }

            Regex regex;
            try
            {
                var options = RegexOptions.CultureInvariant;
                if (!caseSensitive)
                    options |= RegexOptions.IgnoreCase;

                regex = new Regex(pattern!, options);
            }
            catch (ArgumentException ex)
            {
                return Task.FromResult(BridgeToolResult.Failed(
                    request,
                    "InvalidRegex",
                    $"Invalid regular expression: {ex.Message}"));
            }

            var matches = new List<RegexTextSearchMatch>();
            var totalMatchCount = 0;

            for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entryText = entries[entryIndex];
                foreach (Match match in regex.Matches(entryText))
                {
                    totalMatchCount++;
                    if (matches.Count < maxResults)
                    {
                        matches.Add(new RegexTextSearchMatch
                        {
                            EntryIndex = entryIndex,
                            EntryText = entryText,
                            MatchIndex = match.Index,
                            MatchLength = match.Length,
                            Value = match.Value
                        });
                    }
                }
            }

            var data = new Dictionary<string, object?>
            {
                ["matches"] = matches,
                ["matchCount"] = matches.Count,
                ["totalMatchCount"] = totalMatchCount,
                ["limited"] = totalMatchCount > matches.Count
            };

            return Task.FromResult(BridgeToolResult.Succeeded(
                request,
                $"Regex text search completed with {matches.Count} matched result(s).",
                data));
        }

        private static bool TryGetStringArgument(BridgeToolRequest request, string key, out string? value)
        {
            value = null;
            if (!request.Arguments.TryGetValue(key, out var rawValue))
                return false;

            value = rawValue as string;
            return !string.IsNullOrWhiteSpace(value);
        }

        private static IReadOnlyList<string> GetEntries(BridgeToolRequest request)
        {
            if (TryGetStringArgument(request, "inputText", out var inputText))
                return new[] { inputText! };

            if (!request.Arguments.TryGetValue("entries", out var rawEntries) || rawEntries == null)
                return Array.Empty<string>();

            if (rawEntries is IEnumerable<string> stringEntries)
                return stringEntries.Where(entry => entry != null).ToArray();

            if (rawEntries is IEnumerable entries)
            {
                var values = new List<string>();
                foreach (var entry in entries)
                {
                    if (entry is string value)
                        values.Add(value);
                }

                return values;
            }

            return Array.Empty<string>();
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
    }
}
