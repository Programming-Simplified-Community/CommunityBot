using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Data.Challenges;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Dto;

public class ProgrammingChallengeSubmissionDto
{
    [HiddenInput] 
    public int Id { get; set; }

    [Required]
    [DisplayName("User Id")]
    public string UserId { get; set; }

    [DisplayName("Submitted On")]
    public DateTime SubmittedOn { get; set; }
    
    [Required]
    [DisplayName("Discord Guild Id")]
    public string DiscordGuildId { get; set; }

    [Required]
    [DisplayName("Discord Channel Id")]
    public string DiscordChannelId { get; set; }

    public int Attempt { get; set; }

    [HiddenInput]
    public int ProgrammingChallengeId { get; set; }

    [DisplayName("Submitted Language")]
    public ProgrammingLanguage SubmittedLanguage { get; set; } = ProgrammingLanguage.Python;

    [Required]
    [DisplayName("User Submission")]
    public string UserSubmission { get; set; }
}