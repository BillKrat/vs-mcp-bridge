namespace ManualMultiFileEditFixtures;

public static class MultiFileSuccessB
{
    public static string GetSecondaryStatus()
    {
        var status = "pending";
        var owner = "beta";

        return owner + ":" + status;
    }

    /*
        Manual intent:
        - Use this file as the second half of a successful two-file proposal.
        - Example change:
          - change status from "pending" to "archived"
    */
}
