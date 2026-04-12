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

            await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "before\r\nsecond\r\n", "after\r\nsecond\r\n")
            });

            var updated = await File.ReadAllTextAsync(path);
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

            await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "before\nsecond", "after\nsecond")
            });

            var updated = await File.ReadAllTextAsync(path);
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

            await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "before\nsecond\n", "after\nsecond\n")
            });

            var updated = await File.ReadAllTextAsync(path);
            var afterWriteTime = File.GetLastWriteTimeUtc(path);
            Assert.Equal("after\nsecond\n", updated);
            Assert.Equal(beforeWriteTime, afterWriteTime);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_applies_single_file_replacement_exactly_as_intended()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "alpha\nbeta\ngamma\n");
            var applier = new FileEditApplier();

            await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "alpha\nbeta\ngamma\n", "one\ntwo\nthree\n")
            });

            var updated = await File.ReadAllTextAsync(path);
            Assert.Equal("one\ntwo\nthree\n", updated);
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

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", "before\ncontent\n", "after\ncontent\n")
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
