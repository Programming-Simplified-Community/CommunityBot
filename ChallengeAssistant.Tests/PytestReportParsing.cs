using ChallengeAssistant.DockerTypes;

namespace ChallengeAssistant.Tests;

public class Tests
{
    private string _pyTestReportText = default!;
    
    [SetUp]
    public void Setup()
    {
        _pyTestReportText = File.ReadAllText(Path.Join("Files", "pytest-report.json"));
    }

    [Test]
    public async Task TestParse()
    {
        var report = await PythonDockerTest.ParsePytest(_pyTestReportText);

        Assert.NotNull(report);
    }
}