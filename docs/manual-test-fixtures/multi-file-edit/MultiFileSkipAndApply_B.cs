namespace ManualMultiFileEditFixtures;

public static class MultiFileSkipAndApplyB
{
    public static string GetClientState()
    {
        var status = "pending";
        var area = "client";

        return area + ":" + status;
    }

    /*
        Manual intent:
        - Pair with MultiFileSkipAndApply_A.cs for mixed skip/apply validation.
        - Example change:
          - change status from "pending" to "approved"
    */
}
