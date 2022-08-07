using System.ComponentModel.DataAnnotations;
using Data.Challenges;

namespace ChallengeAssistant.Requests;

/// <summary>
/// User submitting their code for testing
/// </summary>
public class SubmitProgrammingChallengeRequest
{
    /// <summary>
    /// Discord User ID of person submitting
    /// </summary>
    [Required]
    public string DiscordUserId { get; set; }

    [Required] 
    public string DiscordChannelId { get; set; }

    [Required] 
    public string DiscordGuildId { get; set; }

    /// <summary>
    /// Discord username of person submitting
    /// </summary>
    [Required]
    public string DiscordUsername { get; set; }
    
    /// <summary>
    /// Challenge to submit code for
    /// </summary>
    public int ProgrammingChallengeId { get; set; }
    
    /// <summary>
    /// Code to test
    /// </summary>
    [Required]
    public string Code { get; set; }

    public ProgrammingLanguage Language { get; set; } = ProgrammingLanguage.Python;
}