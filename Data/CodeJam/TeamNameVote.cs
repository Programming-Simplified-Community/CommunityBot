namespace Data.CodeJam;

public class TeamNameVote : IEntityWithTypedId<int>
{
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key to <see cref="Team"/>
    /// </summary>
    public int TeamId { get; set; }
    public Team Team { get; set; }

    /// <summary>
    /// Suggested team name for <see cref="TeamId"/>
    /// </summary>
    public string ProposedName { get; set; }
    
    /// <summary>
    /// User who proposed the username
    /// </summary>
    public string ProposedByUserId { get; set; }
    
    /// <summary>
    /// Votes associated with this record
    /// </summary>
    public List<UserTeamNameVote> Votes { get; set; } = new();
}

public class UserTeamNameVote
{
    public int Id { get; set; }
    
    /// <summary>
    /// Links to <see cref="TeamNameVote"/>
    /// </summary>
    public int TeamNameVoteId { get; set; }
    
    /// <summary>
    /// Associates vote to user
    /// </summary>
    public string UserId { get; set; }
    
    /// <summary>
    /// Yes/No
    /// </summary>
    public bool Value { get; set; }
}