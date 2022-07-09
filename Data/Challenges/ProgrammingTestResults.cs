namespace Data.Challenges;

public class ProgrammingChallengeReport
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign Key to <see cref="ProgrammingChallenge"/>
    /// </summary>
    public int ProgrammingChallengeId { get; set; }
    public ProgrammingChallenge ProgrammingChallenge { get; set; } = default!;

    /// <summary>
    /// Foreign key to <see cref="SocialUser"/>
    /// </summary>
    public string UserId { get; set; } = default!;
    public SocialUser User { get; set; } = default!;
    
    /// <summary>
    /// Number of points awarded 
    /// </summary>
    public int Points { get; set; }

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