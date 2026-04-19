using System;
using System.Collections.Generic;
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

            var exception = await Assert.ThrowsAsync<AmbiguousEditTargetException>(() => applier.ApplyAsync(new EditProposal
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

            Assert.Equal("Target document contains multiple matches for the approved proposal.", exception.Message);
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

    [Fact]
    public async Task ApplyAsync_applies_multi_range_replacement_and_preserves_untouched_content()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "alpha\nbeta\ngamma\ndelta\nepsilon\n";
            var proposed = "alpha\nBETA\ngamma\nDELTA\nepsilon\n";
            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdits = new List<RangeEdit>(RangeEditBuilder.BuildAll(original, proposed))
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
    public async Task ApplyAsync_throws_when_one_multi_range_segment_drifted_and_leaves_file_unchanged()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "alpha\nbeta\ngamma\ndelta\nepsilon\n";
            var proposed = "alpha\nBETA\ngamma\nDELTA\nepsilon\n";
            var drifted = "alpha\nbeta\ngamma\nDRIFTED\nepsilon\n";
            await File.WriteAllTextAsync(path, drifted);
            var applier = new FileEditApplier();

            await Assert.ThrowsAsync<TargetDocumentDriftException>(() => applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdits = new List<RangeEdit>(RangeEditBuilder.BuildAll(original, proposed))
            }));

            Assert.Equal(drifted, await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_throws_when_one_multi_range_segment_is_ambiguous_and_leaves_file_unchanged()
    {
        var path = Path.GetTempFileName();
        try
        {
            var current = "prefix one\nmiddle\nprefix one\nsuffix\n";
            await File.WriteAllTextAsync(path, current);
            var applier = new FileEditApplier();

            var exception = await Assert.ThrowsAsync<AmbiguousEditTargetException>(() => applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", current, "ignored"),
                RangeEdits = new List<RangeEdit>
                {
                    new()
                    {
                        StartIndex = 7,
                        OriginalSegment = "one",
                        UpdatedSegment = "ONE",
                        PrefixContext = "prefix ",
                        SuffixContext = "\n"
                    },
                    new()
                    {
                        StartIndex = current.IndexOf("suffix", StringComparison.Ordinal),
                        OriginalSegment = "suffix",
                        UpdatedSegment = "tail",
                        PrefixContext = "\n",
                        SuffixContext = "\n"
                    }
                }
            }));

            Assert.Equal("Target document contains multiple matches for the approved proposal.", exception.Message);
            Assert.Equal(current, await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_does_not_partially_apply_multi_range_when_later_range_fails()
    {
        var path = Path.GetTempFileName();
        try
        {
            var current = "one\nmiddle\nthree\n";
            await File.WriteAllTextAsync(path, current);
            var applier = new FileEditApplier();

            await Assert.ThrowsAsync<TargetDocumentDriftException>(() => applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", current, "ONE\nmiddle\nTHREE\n"),
                RangeEdits = new List<RangeEdit>
                {
                    new()
                    {
                        StartIndex = 0,
                        OriginalSegment = "one",
                        UpdatedSegment = "ONE",
                        PrefixContext = string.Empty,
                        SuffixContext = "\n"
                    },
                    new()
                    {
                        StartIndex = current.IndexOf("three", StringComparison.Ordinal),
                        OriginalSegment = "missing",
                        UpdatedSegment = "THREE",
                        PrefixContext = "\n",
                        SuffixContext = "\n"
                    }
                }
            }));

            Assert.Equal(current, await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_applies_adjacent_multi_range_replacements_safely()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "abXYcd\n";
            var proposed = "ABxycd\n";
            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdits = new List<RangeEdit>
                {
                    new()
                    {
                        StartIndex = 0,
                        OriginalSegment = "ab",
                        UpdatedSegment = "AB",
                        PrefixContext = string.Empty,
                        SuffixContext = "XYcd\n"
                    },
                    new()
                    {
                        StartIndex = 2,
                        OriginalSegment = "XY",
                        UpdatedSegment = "xy",
                        PrefixContext = "ab",
                        SuffixContext = "cd\n"
                    }
                }
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
    public async Task ApplyAsync_supports_single_range_proposal_provided_via_range_edits_collection()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "alpha\nbeta\n";
            var proposed = "alpha\nBETA\n";
            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed),
                RangeEdits = new List<RangeEdit>(RangeEditBuilder.BuildAll(original, proposed))
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
    public async Task ApplyAsync_preserves_full_document_fallback_when_no_range_metadata_is_present()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "alpha\nbeta\n";
            var proposed = "alpha\nBETA\n";
            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, proposed)
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
    public async Task ApplyAsync_exactly_applies_multi_range_quoted_string_replacements_after_earlier_length_change()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = """
namespace ManualMultiRangeEditFixtures;

public static class MultiRangeSuccess
{
    public static string BuildSummary()
    {
        var header = "Report Start";
        var firstStatus = "pending";
        var middleNote = "keep-this-line";
        var secondStatus = "pending";
        var footer = "Report End";

        return header
            + " | first:" + firstStatus
            + " | note:" + middleNote
            + " | second:" + secondStatus
            + " | footer:" + footer;
    }
}
""";

            var proposed = """
namespace ManualMultiRangeEditFixtures;

public static class MultiRangeSuccess
{
    public static string BuildSummary()
    {
        var header = "Report Start";
        var firstStatus = "approved";
        var middleNote = "keep-this-line";
        var secondStatus = "archived";
        var footer = "Report End";

        return header
            + " | first:" + firstStatus
            + " | note:" + middleNote
            + " | second:" + secondStatus
            + " | footer:" + footer;
    }
}
""";

            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("MultiRangeSuccess.cs", original, proposed),
                RangeEdits = new List<RangeEdit>(RangeEditBuilder.BuildAll(original, proposed))
            });

            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal(proposed, await File.ReadAllTextAsync(path));
            Assert.DoesNotContain("var secondStatus = \"\"archive\";", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAsync_applies_multiple_files_all_or_nothing_with_mixed_file_metadata()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();
        var thirdPath = Path.GetTempFileName();

        try
        {
            var firstOriginal = "alpha\nbeta\n";
            var firstUpdated = "alpha\nBETA\n";
            var secondOriginal = "one\ntwo\nthree\nfour\n";
            var secondUpdated = "ONE\ntwo\nTHREE\nfour\n";
            var thirdOriginal = "header\nvalue\n";
            var thirdUpdated = "header\nVALUE\n";

            await File.WriteAllTextAsync(firstPath, firstOriginal);
            await File.WriteAllTextAsync(secondPath, secondOriginal);
            await File.WriteAllTextAsync(thirdPath, thirdOriginal);

            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FileEdits = new List<ProposedFileEdit>
                {
                    new()
                    {
                        FilePath = firstPath,
                        Diff = CreateDiff("first.cs", firstOriginal, firstUpdated),
                        RangeEdit = RangeEditBuilder.Build(firstOriginal, firstUpdated)
                    },
                    new()
                    {
                        FilePath = secondPath,
                        Diff = CreateDiff("second.cs", secondOriginal, secondUpdated),
                        RangeEdits = new List<RangeEdit>(RangeEditBuilder.BuildAll(secondOriginal, secondUpdated))
                    },
                    new()
                    {
                        FilePath = thirdPath,
                        Diff = CreateDiff("third.cs", thirdOriginal, thirdUpdated)
                    }
                }
            });

            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal(firstUpdated, await File.ReadAllTextAsync(firstPath));
            Assert.Equal(secondUpdated, await File.ReadAllTextAsync(secondPath));
            Assert.Equal(thirdUpdated, await File.ReadAllTextAsync(thirdPath));
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
            File.Delete(thirdPath);
        }
    }

    [Fact]
    public async Task ApplyAsync_does_not_change_any_file_when_one_multi_file_edit_drifted()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            var firstOriginal = "alpha\nbeta\n";
            var firstUpdated = "alpha\nBETA\n";
            var secondOriginal = "one\ntwo\n";
            var secondUpdated = "ONE\ntwo\n";
            var secondDrifted = "drift\ntwo\n";

            await File.WriteAllTextAsync(firstPath, firstOriginal);
            await File.WriteAllTextAsync(secondPath, secondDrifted);

            var applier = new FileEditApplier();

            await Assert.ThrowsAsync<TargetDocumentDriftException>(() => applier.ApplyAsync(new EditProposal
            {
                FileEdits = new List<ProposedFileEdit>
                {
                    new()
                    {
                        FilePath = firstPath,
                        Diff = CreateDiff("first.cs", firstOriginal, firstUpdated),
                        RangeEdit = RangeEditBuilder.Build(firstOriginal, firstUpdated)
                    },
                    new()
                    {
                        FilePath = secondPath,
                        Diff = CreateDiff("second.cs", secondOriginal, secondUpdated),
                        RangeEdit = RangeEditBuilder.Build(secondOriginal, secondUpdated)
                    }
                }
            }));

            Assert.Equal(firstOriginal, await File.ReadAllTextAsync(firstPath));
            Assert.Equal(secondDrifted, await File.ReadAllTextAsync(secondPath));
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public async Task ApplyAsync_does_not_change_any_file_when_one_multi_file_edit_is_ambiguous()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            var firstOriginal = "alpha\nbeta\n";
            var firstUpdated = "alpha\nBETA\n";
            var secondCurrent = "foo\nmiddle\nfoo\n";

            await File.WriteAllTextAsync(firstPath, firstOriginal);
            await File.WriteAllTextAsync(secondPath, secondCurrent);

            var applier = new FileEditApplier();

            await Assert.ThrowsAsync<AmbiguousEditTargetException>(() => applier.ApplyAsync(new EditProposal
            {
                FileEdits = new List<ProposedFileEdit>
                {
                    new()
                    {
                        FilePath = firstPath,
                        Diff = CreateDiff("first.cs", firstOriginal, firstUpdated),
                        RangeEdit = RangeEditBuilder.Build(firstOriginal, firstUpdated)
                    },
                    new()
                    {
                        FilePath = secondPath,
                        Diff = CreateDiff("second.cs", secondCurrent, "bar\nmiddle\nfoo\n"),
                        RangeEdit = new RangeEdit
                        {
                            StartIndex = 0,
                            OriginalSegment = "foo",
                            UpdatedSegment = "bar",
                            PrefixContext = string.Empty,
                            SuffixContext = "\n"
                        }
                    }
                }
            }));

            Assert.Equal(firstOriginal, await File.ReadAllTextAsync(firstPath));
            Assert.Equal(secondCurrent, await File.ReadAllTextAsync(secondPath));
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public async Task ApplyAsync_rolls_back_earlier_files_when_a_later_file_write_fails()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            var firstOriginal = "alpha\nbeta\n";
            var firstUpdated = "alpha\nBETA\n";
            var secondOriginal = "one\ntwo\n";
            var secondUpdated = "ONE\ntwo\n";

            await File.WriteAllTextAsync(firstPath, firstOriginal);
            await File.WriteAllTextAsync(secondPath, secondOriginal);
            File.SetAttributes(secondPath, File.GetAttributes(secondPath) | FileAttributes.ReadOnly);

            var applier = new FileEditApplier();

            await Assert.ThrowsAnyAsync<Exception>(() => applier.ApplyAsync(new EditProposal
            {
                FileEdits = new List<ProposedFileEdit>
                {
                    new()
                    {
                        FilePath = firstPath,
                        Diff = CreateDiff("first.cs", firstOriginal, firstUpdated),
                        RangeEdit = RangeEditBuilder.Build(firstOriginal, firstUpdated)
                    },
                    new()
                    {
                        FilePath = secondPath,
                        Diff = CreateDiff("second.cs", secondOriginal, secondUpdated),
                        RangeEdit = RangeEditBuilder.Build(secondOriginal, secondUpdated)
                    }
                }
            }));

            Assert.Equal(firstOriginal, await File.ReadAllTextAsync(firstPath));
            Assert.Equal(secondOriginal, await File.ReadAllTextAsync(secondPath));
        }
        finally
        {
            if (File.Exists(secondPath))
                File.SetAttributes(secondPath, FileAttributes.Normal);

            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public async Task ApplyAsync_rollback_touches_only_files_that_were_mutated()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();
        var thirdPath = Path.GetTempFileName();

        try
        {
            var firstOriginal = "alpha\nbeta\n";
            var firstUpdated = "alpha\nBETA\n";
            var secondOriginal = "skip\nvalue\n";
            var secondUpdated = "skip\nVALUE\n";
            var thirdOriginal = "third\nvalue\n";
            var thirdUpdated = "third\nVALUE\n";

            await File.WriteAllTextAsync(firstPath, firstOriginal);
            await File.WriteAllTextAsync(secondPath, secondUpdated);
            await File.WriteAllTextAsync(thirdPath, thirdOriginal);

            var skippedBeforeWriteTime = File.GetLastWriteTimeUtc(secondPath);
            await Task.Delay(1100);

            File.SetAttributes(thirdPath, File.GetAttributes(thirdPath) | FileAttributes.ReadOnly);

            var applier = new FileEditApplier();

            await Assert.ThrowsAnyAsync<Exception>(() => applier.ApplyAsync(new EditProposal
            {
                FileEdits = new List<ProposedFileEdit>
                {
                    new()
                    {
                        FilePath = firstPath,
                        Diff = CreateDiff("first.cs", firstOriginal, firstUpdated),
                        RangeEdit = RangeEditBuilder.Build(firstOriginal, firstUpdated)
                    },
                    new()
                    {
                        FilePath = secondPath,
                        Diff = CreateDiff("second.cs", secondOriginal, secondUpdated),
                        RangeEdit = RangeEditBuilder.Build(secondOriginal, secondUpdated)
                    },
                    new()
                    {
                        FilePath = thirdPath,
                        Diff = CreateDiff("third.cs", thirdOriginal, thirdUpdated)
                    }
                }
            }));

            var skippedAfterWriteTime = File.GetLastWriteTimeUtc(secondPath);
            Assert.Equal(firstOriginal, await File.ReadAllTextAsync(firstPath));
            Assert.Equal(secondUpdated, await File.ReadAllTextAsync(secondPath));
            Assert.Equal(thirdOriginal, await File.ReadAllTextAsync(thirdPath));
            Assert.Equal(skippedBeforeWriteTime, skippedAfterWriteTime);
        }
        finally
        {
            if (File.Exists(thirdPath))
                File.SetAttributes(thirdPath, FileAttributes.Normal);

            File.Delete(firstPath);
            File.Delete(secondPath);
            File.Delete(thirdPath);
        }
    }

    [Fact]
    public async Task ApplyAsync_succeeds_when_one_file_is_already_updated_and_another_requires_apply()
    {
        var firstPath = Path.GetTempFileName();
        var secondPath = Path.GetTempFileName();

        try
        {
            var firstOriginal = "alpha\nbeta\n";
            var firstUpdated = "alpha\nBETA\n";
            var secondOriginal = "one\ntwo\n";
            var secondUpdated = "ONE\ntwo\n";

            await File.WriteAllTextAsync(firstPath, firstUpdated);
            await File.WriteAllTextAsync(secondPath, secondOriginal);

            var firstBeforeWriteTime = File.GetLastWriteTimeUtc(firstPath);
            await Task.Delay(1100);

            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FileEdits = new List<ProposedFileEdit>
                {
                    new()
                    {
                        FilePath = firstPath,
                        Diff = CreateDiff("first.cs", firstOriginal, firstUpdated),
                        RangeEdit = RangeEditBuilder.Build(firstOriginal, firstUpdated)
                    },
                    new()
                    {
                        FilePath = secondPath,
                        Diff = CreateDiff("second.cs", secondOriginal, secondUpdated)
                    }
                }
            });

            var firstAfterWriteTime = File.GetLastWriteTimeUtc(firstPath);
            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal(firstUpdated, await File.ReadAllTextAsync(firstPath));
            Assert.Equal(secondUpdated, await File.ReadAllTextAsync(secondPath));
            Assert.Equal(firstBeforeWriteTime, firstAfterWriteTime);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public async Task ApplyAsync_preserves_single_file_compatibility_when_file_edits_collection_is_absent()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = "before\nsecond\n";
            var updated = "after\nsecond\n";
            await File.WriteAllTextAsync(path, original);
            var applier = new FileEditApplier();

            var result = await applier.ApplyAsync(new EditProposal
            {
                FilePath = path,
                Diff = CreateDiff("sample.cs", original, updated),
                RangeEdit = RangeEditBuilder.Build(original, updated)
            });

            Assert.Equal(EditApplyResult.Applied, result);
            Assert.Equal(updated, await File.ReadAllTextAsync(path));
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
