
namespace ChallengeAssistant.Reports;

/// <summary>
/// Outcome for a Pytest report
/// </summary>
public enum PytestOutcome { Failed, Passed }

/// <summary>
/// Strongly-typed version of a Pytest report in Python
/// </summary>
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
    
    /// <summary>
    /// List of test cases that were ran for this specific test
    /// </summary>
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