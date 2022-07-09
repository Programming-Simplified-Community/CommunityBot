using System.ComponentModel.DataAnnotations;
using Data.CodeJam;

namespace CodeJam.Requests;

/// <summary>
/// A user attempting to register for a particular topic
/// </summary>
public class RegistrationRequest
{
    [Required] public string GuildId { get; set; }

    [Required] public string MemberId { get; set; }

    [Required] public string DisplayName { get; set; }

    public string TopicTitle { get; set; }
    public string Timezone { get; set; }
    public SigmaLevel ExperienceLevel { get; set; }
    public bool IsSolo { get; set; }
}