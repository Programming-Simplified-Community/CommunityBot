using System.Collections.Concurrent;
using System.Text;
using ChallengeAssistant.DockerTypes;
using ChallengeAssistant.Models;
using Data.Challenges;
using Discord;
using Discord.WebSocket;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Razor.Templating.Core;

namespace ChallengeAssistant.Services;

public class CodeRunnerService : BackgroundService, ICodeRunner
{
    private readonly ConcurrentQueue<UserSubmissionQueueItem> _submissionQueue = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));
    private readonly ILogger<CodeRunnerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SocialDbContext _context;
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;

    private readonly int _maxConcurrentRunners;
    private ConcurrentDictionary<string, Task> _runners;

    public int CurrentRunners => _runners?.Count ?? 0;
    
    public CodeRunnerService(
        ILogger<CodeRunnerService> logger, 
        IServiceProvider serviceProvider, 
        SocialDbContext context, 
        IConfiguration config, DiscordSocketClient client)
    {
        _config = config;
        _serviceProvider = serviceProvider;
        _context = context;
        _client = client;
        _logger = logger;
        _maxConcurrentRunners = config["CodeRunner:ConcurrentRunners"] is null
            ? 2
            : config.GetValue<int>("CodeRunner:ConcurrentRunners");

        _runners = new();
    }

    private async Task ProcessItem(Guid taskId, UserSubmissionQueueItem item, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Processing submission from {user}. Language: {Language}.\nCode:\n{Code}",
                item.Submission.UserId,
                item.Test.Language,
                item.Code);

            DockerTest dockerTest;

            switch (item.Test.Language)
            {
                case ProgrammingLanguage.Python:
                    dockerTest = new PythonDockerTest(_serviceProvider.GetRequiredService<ILogger<PythonDockerTest>>(), 
                        _config);
                    break;

                default:
                    throw new NotImplementedException(item.Test.Language.ToString());
            }

            var results = await dockerTest.Start(item.Test, item.Code);

            if (results is not null)
            {
                var passing = results.TestResults.Count(x => x.Result == TestStatus.Pass);
                var total = results.TestResults.Count;
                var allPassing = passing == total;

                results.Points = passing;
                
                var existingReport = await _context.ChallengeReports
                    .Include(x=>x.TestResults)
                    .FirstOrDefaultAsync(x =>
                    x.UserId == item.Submission.UserId
                    && x.ProgrammingChallengeId == item.Submission.ProgrammingChallengeId, stoppingToken);

                if (existingReport is not null)
                {
                    existingReport.Points = passing;
                    _context.TestResults.RemoveRange(existingReport.TestResults);
                    _context.TestResults.AddRange(results.TestResults);
                    results = existingReport;
                }
                else
                {
                    results.UserId = item.Submission.UserId;
                    _context.ChallengeReports.Add(results);
                    _context.TestResults.AddRange(results.TestResults);    
                }

                await _context.SaveChangesAsync(stoppingToken);
                
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == item.Submission.UserId, stoppingToken);

                if (user is null)
                {
                    _logger.LogError("Could not locate user {user}", item.Submission.UserId);
                    throw new Exception("Unable to locate user");
                }

                var embedColor = Color.Red;

                if (allPassing && total > 0)
                    embedColor = Color.Green;
                else if (total == 0)
                    embedColor = Color.Magenta;

                var sb = new StringBuilder();
                if (allPassing && total > 0)
                    sb.AppendLine($"Good job, {user.DiscordDisplayName}! All tests passed!");
                else if (total == 0)
                {
                    sb.AppendLine("Appears no tests were executed. Please make sure you provided valid syntax!");
                }
                else
                {
                    sb.AppendLine("```yml");
                    foreach (var test in results.TestResults)
                        sb.AppendLine($"{test.Name}: {test.Result}");
                    sb.AppendLine("\n```");
                }

                var reportHtml = await RazorTemplateEngine.RenderAsync("~/Views/Shared/TestReport.cshtml", results);

                SubmissionFeedbackEmbedInfo feedback = new(user.DiscordDisplayName, sb.ToString(), $"{passing}/{total}",
                    embedColor);

                OnSubmissionProcessed?.Invoke(feedback);

                ulong.TryParse(item.Submission.DiscordGuildId, out var guildId);
                ulong.TryParse(item.Submission.DiscordChannelId, out var channelId);

                if (guildId <= 0 || channelId <= 0)
                {
                    _logger.LogError("Error: Was unable to parse guild ({GuildId}) or channel ({Channel}) id's",
                        guildId,
                        channelId);
                    throw new Exception("Unable to parse guild or channel id");
                }

                var channel = _client.GetGuild(guildId).GetChannel(channelId) as SocketTextChannel;

                if (channel is null)
                    throw new Exception("Unable to find text channel");
                
                var thread = channel.Threads.FirstOrDefault(x => x.Name == "Test Results");

                if (thread is null)
                    thread = await channel.CreateThreadAsync("Test Results");

                var discordUser = await _client.GetUserAsync(ulong.Parse(user.DiscordUserId!));

                await thread.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle(feedback.Title)
                    .WithDescription(feedback.Description)
                    .WithFooter(feedback.Footer)
                    .WithColor(embedColor).Build());

                // Just to avoid rate limiting... wait a few seconds before sending back-to-back messages
                await Task.Delay(TimeSpan.FromSeconds(2));

                if (results.TestResults.Any())
                {
                    using var reportStream = new MemoryStream();
                    var reportHtmlBytes = Encoding.ASCII.GetBytes(reportHtml);
                    reportStream.Write(reportHtmlBytes);
                    await reportStream.FlushAsync();
                    reportStream.Position = 0;
                    await thread.SendFileAsync(reportStream,
                        $"{user.UserName}-test-results.html",
                        $"{discordUser.Mention}, here's a more detailed test report");
                }

            }
            else
            {
                _logger.LogWarning("Was unable to process {User}'s submission for {Language}",
                    item.Submission.UserId,
                    item.Test.Language);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while processing user submission: {Error}", ex);
        }
        finally
        {
            // Remove our task from the runner because
            // we need to free up space
            if (_runners.ContainsKey(taskId.ToString()))
                _runners.TryRemove(taskId.ToString(), out _);
        }
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var count = _maxConcurrentRunners - _runners.Count;

            for (var i = 0; i < count; i++)
            {
                _submissionQueue.TryDequeue(out var item);
                var taskId = Guid.NewGuid();

                if (item is null) continue;
                
                var task = Task.Run(async () =>
                {
                    await ProcessItem(taskId, item, stoppingToken);
                }, stoppingToken);

                _runners.TryAdd(taskId.ToString(), task);
            }
            
            await _timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    public Task Enqueue(UserSubmissionQueueItem submission)
    {
        _submissionQueue.Enqueue(submission);
        return Task.CompletedTask;
    }

    public int PendingSubmissions => _submissionQueue.Count;
    public event Action<SubmissionFeedbackEmbedInfo>? OnSubmissionProcessed;
}