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

    /*
        Manual intent:
        - Propose two separated replacements in one file.
        - Example:
          - change firstStatus from "pending" to "approved"
          - change secondStatus from "pending" to "archived"
        - Validate untouched content remains unchanged.
    */
}
