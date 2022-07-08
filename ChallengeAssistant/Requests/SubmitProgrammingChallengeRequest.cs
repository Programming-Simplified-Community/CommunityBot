using System.ComponentModel.DataAnnotations;

namespace ChallengeAssistant.Requests;

public class SubmitProgrammingChallengeRequest
{
    [Required]
    public string DiscordUserId { get; set; }
    
    [Required]
    public string DiscordUsername { get; set; }
    
    public int ProgrammingChallengeId { get; set; }
    
    [Required]
    public string Code { get; set; }
}