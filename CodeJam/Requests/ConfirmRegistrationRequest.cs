using System.ComponentModel.DataAnnotations;

namespace CodeJam.Requests;

/// <summary>
/// A user confirming their participation level for a topic
/// </summary>
public class ConfirmRegistrationRequest
{
    [Required] public string GuildId { get; set; }

    [Required] public string MemberId { get; set; }

    [Required] public string Topic { get; set; }

    public bool Confirm { get; set; }
}