namespace ManualMultiRangeEditFixtures;

public static class MultiRangeDriftFailure
{
    public static string BuildSummary()
    {
        var customer = "Ada";
        var phaseOne = "draft";
        var separator = "::";
        var phaseTwo = "draft";

        return customer + separator + phaseOne + separator + phaseTwo;
    }

    /*
        Manual intent:
        - Submit a proposal that updates both phaseOne and phaseTwo.
        - Before approve, manually change only one target in the file.
        - Validate apply fails because the document drifted after proposal creation.
    */
}
