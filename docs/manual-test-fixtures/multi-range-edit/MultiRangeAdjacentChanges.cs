namespace ManualMultiRangeEditFixtures;

public static class MultiRangeAdjacentChanges
{
    public static string BuildToken()
    {
        var token = "abXYcd";
        var suffix = "-done";
        return token + suffix;
    }

    /*
        Manual intent:
        - Propose nearby replacements inside the same token.
        - Example:
          - change "ab" to "AB"
          - change "XY" to "xy"
        - Validate adjacent or near-adjacent ranges apply safely in one file.
    */
}
