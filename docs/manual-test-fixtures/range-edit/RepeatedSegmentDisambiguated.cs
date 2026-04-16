namespace ManualRangeEditFixtures;

public static class RepeatedSegmentDisambiguated
{
    public static string BuildReport()
    {
        var first = "TARGET";
        var second = "TARGET";

        MethodA();
        MethodB();

        return "alpha:" + first + " | omega:" + second;
    }
    public static void MethodA()
    {
        Console.WriteLine("Hello World");
    }

    public static void MethodB()
    {
        Console.WriteLine("Hello Universe");
    }

    /* BillKrat.2026-04-12 Added per ChatGPT guidance 

	2.	Create a proposal intended for the MethodB occurrence only. 
    Created MethodA and MethodB to disambiguate the two identical lines.
      
     */
}
