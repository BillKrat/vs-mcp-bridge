namespace ManualMultiFileEditFixtures;

public static class MultiFileDriftFailureB
{
    public static string GetReviewState()
    {
        var stage = "draft";
        var label = "second-file";

        return label + ":" + stage;
    }

    /*
        Manual intent:
        - Pair with MultiFileDriftFailure_A.cs for multi-file drift validation.
        - Introduce drift after submit and before approve in either file.
        - Validate the proposal fails without mutating the other file.
    */
}
