using Data.Challenges;
using Discord;

namespace ChallengeAssistant.Models;

public record UserSubmissionQueueItem(ProgrammingTest Test, ProgrammingChallengeSubmission Submission, string Code);
public record SubmissionFeedbackEmbedInfo(string Title, string Description, string Footer, Color Color);
