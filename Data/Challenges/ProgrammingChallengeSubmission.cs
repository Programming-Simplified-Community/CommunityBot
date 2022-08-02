namespace Data.Challenges;

public class ProgrammingChallengeSubmission : IEntityWithTypedId<int>
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
    /// Number of current attempts for this challenge
    /// </summary>
    public int Attempt { get; set; } = 1;

    /// <summary>
    /// Foreign key to <see cref="ProgrammingChallenge"/>
    /// </summary>
    public int ProgrammingChallengeId { get; set; }
    public ProgrammingChallenge ProgrammingChallenge { get; set; }

    /// <summary>
    /// Language in which this user attempted the challenge with
    /// </summary>
    public ProgrammingLanguage SubmittedLanguage { get; set; } = ProgrammingLanguage.Python;

    /// <summary>
    /// Content the user gave
    /// </summary>
    /// <remarks>
    /// We will not add a new record per submission because we're not a data company....
    /// If a user resubmits... it'll override their previous entry
    /// </remarks>
    public string UserSubmission { get; set; } = default!;
}