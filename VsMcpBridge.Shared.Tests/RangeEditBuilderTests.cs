using VsMcpBridge.Shared.Services;
using Xunit;

namespace VsMcpBridge.Shared.Tests;

public sealed class RangeEditBuilderTests
{
    [Fact]
    public void BuildAll_returns_multiple_ranges_for_separated_changes_in_one_file()
    {
        var original = "alpha\nbeta\ngamma\ndelta\nepsilon\n";
        var proposed = "alpha\nBETA\ngamma\nDELTA\nepsilon\n";

        var rangeEdits = RangeEditBuilder.BuildAll(original, proposed);

        Assert.Equal(2, rangeEdits.Count);
        Assert.Equal("beta", rangeEdits[0].OriginalSegment);
        Assert.Equal("BETA", rangeEdits[0].UpdatedSegment);
        Assert.Equal("delta", rangeEdits[1].OriginalSegment);
        Assert.Equal("DELTA", rangeEdits[1].UpdatedSegment);
    }

    [Fact]
    public void BuildAll_preserves_exact_updated_segments_for_later_quoted_string_ranges_after_earlier_length_change()
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

        var rangeEdits = RangeEditBuilder.BuildAll(original, proposed);

        Assert.Equal(2, rangeEdits.Count);
        Assert.Equal("pending", rangeEdits[0].OriginalSegment);
        Assert.Equal("approved", rangeEdits[0].UpdatedSegment);
        Assert.Equal("pending", rangeEdits[1].OriginalSegment);
        Assert.Equal("archived", rangeEdits[1].UpdatedSegment);
        Assert.DoesNotContain("\"archive", rangeEdits[1].UpdatedSegment);
    }
}
