using System.ComponentModel.DataAnnotations;

namespace Data.CodeJam;

public class Team : IEntityWithTypedId<int>
{
    public int Id { get; set; }

    /// <summary>
    /// Team Name
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// <see cref="Topic"/> Id
    /// </summary>
    public int TopicId { get; set; }

    /// <summary>
    /// Discord channel id (ulong as string)
    /// </summary>
    [Required]
    public string TeamChannelId { get; set; }

    /// <summary>
    /// Discord role id (ulong as string)
    /// </summary>
    [Required] 
    public string RoleId { get; set; }

    public List<TeamMember> Members { get; set; }
}