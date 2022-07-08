using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ChallengeAssistant.Reports;

public enum PytestOutcome { Failed, Passed }

public class PytestReport
{
    /// <summary>
    /// Length of time it took to run these tests
    /// </summary>
    public decimal Duration { get; set; }
    
    /// <summary>
    /// Summary indicating number of failed/passed/total
    /// </summary>
    public PytestSummary Summary { get; set; }
    
    public List<PytestItem> Tests { get; set; } = new();
}

public class PytestItem
{
    public PytestOutcome Outcome { get; set; }
    public string TestName { get; set; }
}

public class PytestSummary
{
    public int Failed { get; set; }
    public int Passed { get; set; }
    public int Total { get; set; }
}