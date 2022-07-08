namespace Data.Challenges;

public class ProgrammingChallengeReport
{
    public int Id { get; set; }

    public int ProgrammingChallengeId { get; set; }
    public ProgrammingChallenge ProgrammingChallenge { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public SocialUser User { get; set; } = default!;
    public int Points { get; set; }

    public List<ProgrammingTestResult> TestResults { get; set; } = new();
}

public class ProgrammingTestResult
{
    public int Id { get; set; }
    public int ProgrammingChallengeReportId { get; set; }

    public string Name { get; set; } = default!;
    public TestStatus Result { get; set; }
}

public enum TestStatus
{
    Pass,
    Fail
}