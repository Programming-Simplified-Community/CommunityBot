using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Data.Challenges;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Dto;

public class ProgrammingTestDto 
{
    [HiddenInput]
    public int Id { get; set; }
    
    [DisplayName("Timeout in Minutes")]    
    public int? TimeoutInMinutes { get; set; }

    public ProgrammingLanguage Language { get; set; }

    [Required]
    [DisplayName("Test Docker Image")]
    public string TestDockerImage { get; set; }

    [Required]
    [DisplayName("Docker Entrypoint")]
    public string DockerEntryPoint { get; set; }

    [Required]
    [DisplayName("Executable File-mount Destination")]
    public string ExecutableFileMountDestination { get; set; }
}