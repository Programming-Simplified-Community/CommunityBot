using ChallengeAssistant.Reports;
using ChallengeAssistant.Services;
using Core.Storage;
using Data.Challenges;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace ChallengeAssistant.DockerTypes;

public class PythonDockerTest : DockerTest
{
    public PythonDockerTest(ILogger<DockerTest> logger) : base(logger)
    {
    }

    protected override async Task<string> CreateUserCodeFile(string userCode)
    {
        var filename = Guid.NewGuid() + ".py";
        var filePath = Path.Join(AppStorage.ScriptsPath, filename);

        if (!Directory.Exists(AppStorage.ScriptsPath))
            Directory.CreateDirectory(AppStorage.ScriptsPath);
        
        await File.WriteAllTextAsync(filePath, userCode);
        return filePath;
    }

    protected override async Task<ProgrammingChallengeReport?> ParseReport()
    {
        var files = Directory.GetFiles(ReportsDir);

        if (!files.Any())
            return null;

        var results = await ParsePytest(await File.ReadAllTextAsync(files.First()));

        if (results is null) return null;

        var report = new ProgrammingChallengeReport
        {
            ProgrammingChallengeId = Test.ProgrammingChallengeId
        };

        foreach (var test in results.Tests)
        {
            report.TestResults.Add(new()
            {
                Name = test.TestName,
                ProgrammingChallengeReportId = Test.ProgrammingChallengeId,
                Result = test.Outcome == PytestOutcome.Passed ? TestStatus.Pass : TestStatus.Fail
            });
        }

        return report;
    }

    protected override async Task Cleanup()
    {
        await base.Cleanup();
        await Util.DeleteDir(AppStorage.ScriptsPath);
    }
    
    public static Task<PytestReport?> ParsePytest(string jsonText)
    {
        if (string.IsNullOrEmpty(jsonText))
            return Task.FromResult<PytestReport?>(null);

        try
        {
            var root = JObject.Parse(jsonText);

            PytestReport report = new();

            int.TryParse(root["summary"]!["passed"]?.ToString(), out var passed);
            int.TryParse(root["summary"]!["failed"]?.ToString(), out var failed);
            int.TryParse(root["summary"]!["total"]?.ToString(), out var total);

            report.Summary = new PytestSummary
            {
                Failed = failed,
                Passed = passed,
                Total = total
            };

            report.Tests = new();

            var testArray = (JArray)root["tests"]!;

            foreach (var item in testArray)
            {
                var test = new PytestItem
                {
                    Outcome = item["outcome"]?.ToString() switch
                    {
                        "passed" => PytestOutcome.Passed,
                        _ => PytestOutcome.Failed
                    }
                };

                var id = item["nodeid"]!.ToObject<string>();

                if (string.IsNullOrEmpty(id)) continue;
                
                var start = id.IndexOf(':') + 2;
                var end = id.IndexOf('[');

                test.TestName = end < 0
                    ? id.Substring(start)
                    : id.Substring(start, Math.Max(end-start,1));

                report.Tests.Add(test);
            }

            return Task.FromResult(report);
        }
        catch (Exception)
        {
            return Task.FromResult<PytestReport?>(null);
        }
    }
}