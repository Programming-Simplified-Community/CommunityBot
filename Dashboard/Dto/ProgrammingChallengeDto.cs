using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Dto;

public class ProgrammingChallengeDto
{
    [HiddenInput]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; }
    
    [Required]
    public string Description { get; set; }
    
    [Required]
    [DisplayName("Query Parameter")]
    public string QueryParameter { get; set; }

    [Required]
    [DisplayName("Is Timed")]
    public bool IsTimed { get; set; }
}