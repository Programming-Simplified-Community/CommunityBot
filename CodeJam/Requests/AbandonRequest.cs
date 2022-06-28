using System.ComponentModel.DataAnnotations;

namespace CodeJam.Requests;

public class AbandonRequest
{
    [Required] public string GuildId { get; set; }
    [Required] public string MemberId { get; set; }
    [Required] public string DisplayName { get; set; }
    [Required] public string Topic { get; set; }
}