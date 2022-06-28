using System.ComponentModel.DataAnnotations;

namespace CodeJam.Requests;

public class SubmissionRequest
{
    [Required] public string GuildId { get; set; }
    [Required] public string MemberId { get; set; }
    [Required] public string DisplayName { get; set; }
    [Required] public string GithubRepo { get; set; }
    [Required] public string Topic { get; set; }
}