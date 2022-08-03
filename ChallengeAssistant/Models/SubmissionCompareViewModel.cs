using Data.Challenges;

namespace ChallengeAssistant.Models;

public record UserSubmission(string Username, string Submission, ProgrammingLanguage Language);

public record MySubmission(string Submission, ProgrammingLanguage Language);

public class SubmissionCompareViewModel
{
    /// <summary>
    /// Key is challenge title, value is submitted code
    /// </summary>
    public Dictionary<string, MySubmission> MySubmissions = new();

    /// <summary>
    /// Compare <see cref="MySubmission"/>'s key to another user's submissions
    /// </summary>
    public Dictionary<string, List<UserSubmission>> UserSubmissions = new();
    
}