namespace Data.CodeJam;

public class TeamMember
{
    /// <summary>
    /// Primary Key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to <see cref="Team"/>
    /// </summary>
    public int TeamId { get; set; }

    /// <summary>
    /// Discord member ID (ulong as string)
    /// </summary>
    public string MemberId { get; set; }
}