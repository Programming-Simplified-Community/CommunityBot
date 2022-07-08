using ChallengeAssistant.Models;
using Microsoft.Extensions.Hosting;

namespace ChallengeAssistant.Services;

public interface ICodeRunner : IHostedService
{
    /// <summary>
    /// Queue submission for review/testing
    /// </summary>
    /// <param name="submission"></param>
    /// <returns></returns>
    Task Enqueue(UserSubmissionQueueItem submission);

    /// <summary>
    /// Number of submissions pending review
    /// </summary>
    int PendingSubmissions { get; }

    /// <summary>
    /// Number of active runners
    /// </summary>
    int CurrentRunners { get; }

    event Action<SubmissionFeedbackEmbedInfo> OnSubmissionProcessed;
}