namespace Data.CodeJam;

public class Submission
{
    public int Id { get; set; }

    /// <summary>
    /// Team this submission is for (if applicable)
    /// </summary>
    public int? TeamId { get; set; }
    
    /// <summary>
    /// User who submitted 
    /// </summary>
    public string DiscordUserId { get; set; }

    /// <summary>
    /// Topic in which the submission is for
    /// </summary>
    public int TopicId { get; set; }

    /// <summary>
    /// Link to a user/team submission for a topic
    /// </summary>
    public string GithubRepo { get; set; }
}