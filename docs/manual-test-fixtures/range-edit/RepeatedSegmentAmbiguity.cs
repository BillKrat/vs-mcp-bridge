namespace ManualRangeEditFixtures;

public static class RepeatedSegmentAmbiguity
{
    public static string BuildReport()
    {
        var first = "TARGET";
        var second = "TARGET";

        return first + " | middle | " + second;
    }
}
