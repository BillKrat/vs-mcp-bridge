namespace ManualMultiRangeEditFixtures;

public static class MultiRangeAmbiguityReference
{
    public static string BuildSummary()
    {
        var left = "TARGET";
        var divider = "|";
        var right = "TARGET";

        Console.WriteLine("repeat-value");
        Console.WriteLine("stable-middle");
        Console.WriteLine("repeat-value");

        return left + divider + right;
    }

    /*
        Manual intent:
        - Reference fixture for ambiguity when repeated content weakens targeting.
        - This is primarily an automated safety proof.
        - Use live only if the UI naturally produces weak enough proposal metadata.
    */
}
