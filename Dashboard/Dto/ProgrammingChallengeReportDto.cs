using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Data.Challenges;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Dto;

public class ProgrammingChallengeReportDto
{
    [HiddenInput] public int Id { get; set; }

    [HiddenInput] public int ProgrammingChallengeId { get; set; }
    
    [Required, DisplayName("User Id")]
    public string UserId { get; set; }

    public int Points { get; set; }

    public string Duration { get; set; }
}

public class ProgrammingTestResultDto
{
    [HiddenInput]
    public int Id { get; set; }
    
    [HiddenInput]
    public int ProgrammingChallengeReportId { get; set; }

    [Required]
    public string Name { get; set; }

    public string? Duration { get; set; }

    [DisplayName("Total Runs")]
    public int TotalRuns { get; set; }

    [DisplayName("Total Fails")]
    public int TotalFails { get; set; }
    
    [DisplayName("Parameters")]
    public string? IncomingValues { get; set; }

    [DisplayName("Assertion Message")]
    public string? AssertionMessage { get; set; }

    public TestStatus Result { get; set; }
}