using System.ComponentModel.DataAnnotations;
using Data.CodeJam;

namespace CodeJam.Requests;

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