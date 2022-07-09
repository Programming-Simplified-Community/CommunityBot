using System.ComponentModel.DataAnnotations;

namespace CodeJam.Requests;

/// <summary>
/// User request to abandon a code jam
/// </summary>
public class AbandonRequest
{
    [Required] public string GuildId { get; set; }
    [Required] public string MemberId { get; set; }
    [Required] public string DisplayName { get; set; }
    [Required] public string Topic { get; set; }
}