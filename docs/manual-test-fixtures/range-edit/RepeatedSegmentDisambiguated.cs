namespace ManualRangeEditFixtures;

public static class RepeatedSegmentDisambiguated
{
    public static string BuildReport()
    {
        var first = "TARGET";
        var second = "TARGET";

        return "alpha:" + first + " | omega:" + second;
    }
}
