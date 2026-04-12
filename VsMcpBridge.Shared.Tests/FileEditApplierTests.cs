using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VsMcpBridge.Shared.Models;
using VsMcpBridge.Shared.Services;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class FileEditApplierTests
{
    [Fact]
    public async Task ApplyAsync_preserves_crlf_line_endings()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "before\r\nsecond\r\n");
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "before\r\nsecond\r\n", "after\r\nsecond\r\n"),
                RangeEdit = RangeEditBuilder.Build("before\r\nsecond\r\n", "after\r\nsecond\r\n")
            });

            var updated = await File.ReadAllTextAsync(path);
            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal("after\r\nsecond\r\n", updated);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_preserves_absence_of_final_trailing_newline()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "before\nsecond");
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "before\nsecond", "after\nsecond"),
                RangeEdit = RangeEditBuilder.Build("before\nsecond", "after\nsecond")
            });

            var updated = await File.ReadAllTextAsync(path);
            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal("after\nsecond", updated);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_does_not_rewrite_when_target_already_matches_updated_text()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "after\nsecond\n");
            var applier = new FileEditApplier();
            var beforeWriteTime = File.GetLastWriteTimeUtc(path);
            await Task.Delay(1100);

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "before\nsecond\n", "after\nsecond\n"),
                RangeEdit = RangeEditBuilder.Build("before\nsecond\n", "after\nsecond\n")
            });

            var updated = await File.ReadAllTextAsync(path);
            var afterWriteTime = File.GetLastWriteTimeUtc(path);
            Assert.Equal(EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent, result);
            Assert.Equal("after\nsecond\n", updated);
            Assert.Equal(beforeWriteTime, afterWriteTime);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_applies_single_range_replacement_and_preserves_surrounding_content()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "alpha\nbeta\ngamma\ndelta\n");
            var applier = new FileEditApplier();
            var original = "alpha\nbeta\ngamma\ndelta\n";
            var updated = "alpha\none\ntwo\ndelta\n";

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, updated),
                RangeEdit = RangeEditBuilder.Build(original, updated)
            });

            var appliedText = await File.ReadAllTextAsync(path);
            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal("alpha\none\ntwo\ndelta\n", appliedText);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_throws_and_leaves_file_unchanged_when_target_document_has_drifted()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "drifted\ncontent\n");
            var applier = new FileEditApplier();

            var exception = await Assert.ThrowsAsync<TargetDocumentDriftException>(() => applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "before\ncontent\n", "after\ncontent\n"),
                RangeEdit = RangeEditBuilder.Build("before\ncontent\n", "after\ncontent\n")
            }));

            var current = await File.ReadAllTextAsync(path);
            Assert.Equal("Target document no longer matches the approved proposal.", exception.Message);
            Assert.Equal("drifted\ncontent\n", current);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_throws_when_target_range_changed_even_if_surrounding_document_exists()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "header\nbefore\ncontent\nfooter\n";
            var proposed = "header\nafter\ncontent\nfooter\n";
            await File.WriteAllTextAsync(path, "header\ndrifted\ncontent\nfooter\n");
            var applier = new FileEditApplier();

            await Assert.ThrowsAsync<TargetDocumentDriftException>(() => applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdit = RangeEditBuilder.Build(original, proposed)
            }));

            Assert.Equal("header\ndrifted\ncontent\nfooter\n", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_fails_when_range_match_is_ambiguous()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "foo\nmiddle\nfoo\n");
            var applier = new FileEditApplier();

            await Assert.ThrowsAsync<TargetDocumentDriftException>(() => applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "foo\nmiddle\nfoo\n", "bar\nmiddle\nfoo\n"),
                RangeEdit = new RangeEdit
                {
                    StartIndex = 0,
                    OriginalSegment = "foo",
                    UpdatedSegment = "bar",
                    PrefixContext = string.Empty,
                    SuffixContext = "\n"
                }
            }));

            Assert.Equal("foo\nmiddle\nfoo\n", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_applies_insertion_only_range()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "alpha\ngamma\n";
            var proposed = "alpha\nbeta\ngamma\n";
            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdit = RangeEditBuilder.Build(original, proposed)
            });

            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal(proposed, await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_applies_deletion_only_range()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "alpha\nbeta\ngamma\n";
            var proposed = "alpha\ngamma\n";
            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdit = RangeEditBuilder.Build(original, proposed)
            });

            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal(proposed, await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_applies_start_of_file_replacement()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "alpha\nbeta\n";
            var proposed = "start\nbeta\n";
            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdit = RangeEditBuilder.Build(original, proposed)
            });

            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal(proposed, await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_applies_end_of_file_replacement()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "alpha\nbeta\n";
            var proposed = "alpha\nfinish\n";
            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdit = RangeEditBuilder.Build(original, proposed)
            });

            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal(proposed, await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_skips_when_updated_segment_is_already_present_at_intended_location()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "header\nbefore\nfooter\n";
            var proposed = "header\nafter\nfooter\n";
            await File.WriteAllTextAsync(path, proposed);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdit = RangeEditBuilder.Build(original, proposed)
            });

            Assert.Equal(EditApplyResult.SkippedAlreadyMatchesApprovedUpdatedContent, result);
            Assert.Equal(proposed, await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateDiff(string filePath, string original, string proposed)
    {
        var originalLines = original.Split('\n');
        var proposedLines = proposed.Split('\n');

        var sb = new StringBuilder();
        sb.AppendLine($"--- a/{filePath}");
        sb.AppendLine($"+++ b/{filePath}");

        int i = 0;
        int j = 0;
        while (i < originalLines.Length || j < proposedLines.Length)
        {
            if (i < originalLines.Length && j < proposedLines.Length && originalLines[i] == proposedLines[j])
            {
                sb.AppendLine($" {originalLines[i]}");
                i++;
                j++;
            }
            else if (i < originalLines.Length)
            {
                sb.AppendLine($"-{originalLines[i]}");
                i++;
            }
            else
            {
                sb.AppendLine($"+{proposedLines[j]}");
                j++;
            }
        }

        return sb.ToString();
    }
}
