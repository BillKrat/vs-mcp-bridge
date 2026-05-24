namespace ManualRangeEditFixtures;

public static class RepeatedSegmentAmbiguity
{
    public static string BuildReport()
    {
        var first = "TARGET";
        var second = "TARGET";

        Console.WriteLine("Hello World");

        Console.WriteLine("Hello World");

        return first + " | middle | " + second;
    }

    /* BillKrat.2026-04-12 Added per ChatGPT guidance 

	2.	Use the proposal UI to target changing only 
        one of the two identical lines:

	•	from Console.WriteLine("Hello World");
	•	to Console.WriteLine("Hello Universe");      
      
     */
}
