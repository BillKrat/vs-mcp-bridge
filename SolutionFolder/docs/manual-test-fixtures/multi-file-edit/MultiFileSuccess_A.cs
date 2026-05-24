namespace ManualMultiFileEditFixtures;

public static class MultiFileSuccessA
{
    public static string GetPrimaryStatus()
    {
        var status = "pending";
        var owner = "alpha";

        return owner + ":" + status;
    }

    /*
        Manual intent:
        - Use this file as the first half of a successful two-file proposal.
        - Example change:
          - change status from "pending" to "approved"
    */
}
