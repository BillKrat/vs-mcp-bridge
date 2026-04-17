namespace ManualMultiFileEditFixtures;

public static class MultiFileDriftFailureA
{
    public static string GetDraftState()
    {
        var stage = "draft";
        var label = "first-file";

        return label + ":" + stage;
    }

    /*
        Manual intent:
        - Submit a two-file proposal involving this file and MultiFileDriftFailure_B.cs.
        - Before approval, edit one of the target files so the approved proposal drifts.
        - Validate no partial apply occurs.
    */
}
