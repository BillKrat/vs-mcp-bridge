// 2026-04-12
// Comments here
namespace ManualRangeEditFixtures;

public static class StartOfFileReplacement
{
    public static string BuildHeader()
    {
        return "OLD-HEADER\n" +
               "line-1\n" +
               "line-2\n";
    }
}
