namespace Data.Challenges;

public class ProgrammingChallengeSubmission
{
    public int Id { get; set; }

    /// <summary>
    /// <see cref="SocialUser"/> PK
    /// </summary>
    public string UserId { get; set; } = default!;

    /// <summary>
    /// Time submitted
    /// </summary>
    public DateTime SubmittedOn { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Guild in which this submission came from
    /// </summary>
    public string DiscordGuildId { get; set; } = default!;
    
    /// <summary>
    /// Channel in which this submission came from
    /// </summary>
    public string DiscordChannelId { get; set; } = default!;

    /// <summary>
    /// Foreign key to <see cref="ProgrammingChallenge"/>
    /// </summary>
    public int ProgrammingChallengeId { get; set; }
    public ProgrammingChallenge ProgrammingChallenge { get; set; }

    /// <summary>
    /// Content the user gave
    /// </summary>
    /// <remarks>
    /// We will not add a new record per submission because we're not a data company....
    /// If a user resubmits... it'll override their previous entry
    /// </remarks>
    public string UserSubmission { get; set; } = default!;
}