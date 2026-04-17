namespace ManualMultiFileEditFixtures;

public static class MultiFileSkipAndApplyA
{
    public static string GetServerState()
    {
        var status = "approved";
        var area = "server";

        return area + ":" + status;
    }

    /*
        Manual intent:
        - Keep this file already matching the approved updated content.
        - Pair with MultiFileSkipAndApply_B.cs, which should still require apply.
    */
}
