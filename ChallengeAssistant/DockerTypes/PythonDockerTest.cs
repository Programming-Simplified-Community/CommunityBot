using ChallengeAssistant.Reports;
using Core.Storage;
using Data.Challenges;
using Newtonsoft.Json.Linq;

namespace ChallengeAssistant.DockerTypes;

/// <summary>
/// Container to assist with storing data from PyTest
/// </summary>
internal class PytestContainer
{
    public List<double> Duration { get; } = new();
    public List<string> Assertions { get; } = new();
    public List<string> Incoming { get; }= new();
    public string Name { get; set; }
    public int Total { get; set; }
    public int Failed { get; set; }
    public bool Passed => Total > 0 && Failed == 0;
}

/// <summary>
/// Handles running Python code from our users
/// </summary>
public class PythonDockerTest : DockerTest
{
    public PythonDockerTest(ILogger<DockerTest> logger, IConfiguration config) : base(logger, config)
    {
    }

    /// <remarks>
    /// This may later change, where we inject code into the docker file? We'll see.
    /// </remarks>
    /// <param name="userCode"></param>
    /// <returns></returns>
    protected override async Task<string> CreateUserCodeFile(string userCode)
    {
        var filename = Guid.NewGuid() + ".py";
        var filePath = Path.Join(AppStorage.ScriptsPath, filename);

        if (!Directory.Exists(AppStorage.ScriptsPath))
            Directory.CreateDirectory(AppStorage.ScriptsPath);
        
        await File.WriteAllTextAsync(filePath, userCode);
        return filePath;
    }

    /// <summary>
    /// Parse our 
    /// </summary>
    /// <returns></returns>
    protected override async Task<ProgrammingChallengeReport?> ParseReport()
    {
        var files = Directory.GetFiles(AppStorage.ReportsPath);

        if (!files.Any())
        {
            Logger.LogWarning("Could not locate any report files at {Path}", AppStorage.ReportsPath);
            return null;
        }
        
        var results = await ParsePytest(await File.ReadAllTextAsync(files.First()));

        if (results is null)
        {
            Logger.LogWarning("Something went wrong while trying to parse the report file...\n\t{file}", files.First());
            return null;
        }
        
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
                Result = test.Outcome == PytestOutcome.Passed ? TestStatus.Pass : TestStatus.Fail,
                Duration = test.CallDuration,
                AssertionMessage = test.AssertionMessage,
                IncomingValues = test.IncomingValues,
                TotalFails = test.Failed,
                TotalRuns = test.Total
            });
        }

        return report;
    }

    protected override async Task<string> CreateDockerFile(string userCodeFilePath)
    {
        Dictionary<string, string> vars = new()
        {
            ["IMAGE"] = Test.TestDockerImage,
            ["USER_FILE"] = userCodeFilePath,
            ["DESTINATION_FILE"] = Test.ExecutableFileMountDestination
        };

        return await Util.CreateDockerFileFromTemplate(Test.Language, vars);
    }

    protected override async Task Cleanup()
    {
        await base.Cleanup();
        await Util.DeleteDir(AppStorage.ScriptsPath);
    }

    public static async Task<ProgrammingChallengeReport?> ParseReport(PytestReport? pytestReport, int challengeId=1)
    {
        if (pytestReport is null)
            return null;

        var report = new ProgrammingChallengeReport
        {
            ProgrammingChallengeId = challengeId
        };

        foreach (var test in pytestReport.Tests)
        {
            report.TestResults.Add(new()
            {
                Name = test.TestName,
                ProgrammingChallengeReportId = challengeId,
                Result = test.Outcome == PytestOutcome.Passed ? TestStatus.Pass : TestStatus.Fail,
                Duration = test.CallDuration,
                AssertionMessage = test.AssertionMessage,
                IncomingValues = test.IncomingValues,
                TotalFails = test.Failed,
                TotalRuns = test.Total
            });
        }

        return report;
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

            // We need a way of tracking multiple test-cases for the same test
            Dictionary<string, PytestContainer> namedTests = new();
            
            foreach (var item in testArray)
            {
                var id = item["nodeid"]!.ToObject<string>();

                if (string.IsNullOrEmpty(id)) continue;
                
                var start = id.IndexOf(':') + 2;
                var end = id.IndexOf('[');
                
                var testName = end < 0
                    ? id.Substring(start)
                    : id.Substring(start, Math.Max(end-start,1));

                if (!namedTests.ContainsKey(testName))
                    namedTests.Add(testName, new()
                    {
                        Name = testName
                    });
                
                var outcome = item["outcome"]?.ToString() switch
                {
                    "passed" => PytestOutcome.Passed,
                    _ => PytestOutcome.Failed
                };

                if (outcome == PytestOutcome.Failed)
                {
                    namedTests[testName].Failed += 1;
                    namedTests[testName].Assertions
                        .Add(item["call"]?["crash"]?["message"]?.ToObject<string>() ?? string.Empty);

                    var longrep = item["call"]?["longrepr"]?.ToObject<string>() ?? string.Empty;
                    
                    if (!string.IsNullOrWhiteSpace(longrep))
                    {
                        var parameterEnd = longrep.IndexOf("\n\n");
                        longrep = longrep[..parameterEnd];
                    }
                    
                    namedTests[testName].Incoming.Add(longrep);
                }
                
                namedTests[testName].Total++;
                namedTests[testName].Duration.Add(item["call"]?["duration"]?.ToObject<double>() ?? -1);
            }

            foreach (var container in namedTests.Values)
                report.Tests.Add(new()
                {
                    TestName = container.Name,
                    Outcome = container.Passed ? PytestOutcome.Passed : PytestOutcome.Failed,
                    AssertionMessage = string.Join("|||", container.Assertions),
                    CallDuration = container.Duration.Sum()/container.Duration.Count,
                    IncomingValues = string.Join("|||", container.Incoming),
                    Failed = container.Failed,
                    Total = container.Total
                });
            
            return Task.FromResult<PytestReport?>(report);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return Task.FromResult<PytestReport?>(null);
        }
    }
}