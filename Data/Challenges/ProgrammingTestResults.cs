using System.Text.Json.Serialization;

namespace Data.Challenges;

public class ProgrammingChallengeReport : IEntityWithTypedId<int>
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="ProgrammingChallenge"/>
    /// </summary>
    public int ProgrammingChallengeId { get; set; }
    
    [JsonIgnore]
    public ProgrammingChallenge ProgrammingChallenge { get; set; } = default!;

    /// <summary>
    /// Foreign key to <see cref="SocialUser"/>
    /// </summary>
    public string UserId { get; set; } = default!;
    
    [JsonIgnore]
    public SocialUser User { get; set; } = default!;
    
    /// <summary>
    /// Number of points awarded 
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Total time it took to execute these tests
    /// </summary>
    public string Duration { get; set; }

    /// <summary>
    /// Test results associated with this report
    /// </summary>
    public List<ProgrammingTestResult> TestResults { get; set; } = new();
}

public class ProgrammingTestResult
{
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key to <see cref="ProgrammingChallengeReport"/>
    /// </summary>
    public int ProgrammingChallengeReportId { get; set; }
    
    /// <summary>
    /// Test name
    /// </summary>
    public string Name { get; set; } = default!;

    public string? Duration { get; set; }

    /// <summary>
    /// Total number of runs for this method/test case
    /// </summary>
    public int TotalRuns { get; set; }
    
    /// <summary>
    /// Total number of failed runs for this method/test case
    /// </summary>
    public int TotalFails { get; set; }

    /// <summary>
    /// The values that were passed in that lead to a failure
    /// </summary>
    /// <remarks>
    ///     This will be delimited by triple pipes |||
    /// </remarks>
    public string? IncomingValues { get; set; }
    
    /// <summary>
    /// Message that showcases what went wrong
    /// </summary>
    /// <remarks>
    ///     This wil be delimited by triple pipes |||
    /// </remarks>
    public string? AssertionMessage { get; set; }

    /// <summary>
    /// Pass/Fail
    /// </summary>
    public TestStatus Result { get; set; }
}

/// <summary>
/// Indicates a pass/fail
/// </summary>
public enum TestStatus
{
    Pass,
    Fail
}