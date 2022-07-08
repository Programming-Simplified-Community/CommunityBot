namespace Data.Challenges;

public enum ProgrammingLanguage
{
    Python
}

public class ProgrammingChallenge
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Question { get; set; } = default!;
    public string Explanation { get; set; } = default!;
    public string? Tip { get; set; } = default!;
    public List<ProgrammingTest> Tests { get; set; } = new();
}

public class ProgrammingTest
{
    public int Id { get; set; }
    public int ProgrammingChallengeId { get; set; }

    public ProgrammingLanguage Language { get; set; }

    /// <summary>
    /// Docker image to utilize for things
    /// </summary>
    public string TestDockerImage { get; set; } = default!;

    /// <summary>
    /// This overrides the entrypoint on <see cref="TestDockerImage"/>
    /// </summary>
    public string DockerEntryPoint { get; set; } = default!;

    /// <summary>
    /// Path in which to mount a file to within <see cref="TestDockerImage"/>
    /// </summary>
    public string ExecutableFileMountDestination { get; set; } = default!;
}