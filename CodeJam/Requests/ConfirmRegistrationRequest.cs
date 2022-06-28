using System.ComponentModel.DataAnnotations;

namespace CodeJam.Requests;

public class ConfirmRegistrationRequest
{
    [Required] public string GuildId { get; set; }

    [Required] public string MemberId { get; set; }

    [Required] public string Topic { get; set; }

    public bool Confirm { get; set; }
}