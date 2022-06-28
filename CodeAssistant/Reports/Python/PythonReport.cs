using System.Diagnostics;
using Core;
using Core.Storage;
using Razor.Templating.Core;

namespace CodeAssistant.Reports.Python;

internal class PythonReport : ReportBase
{
    private readonly PythonOutput _report;

    public PythonReport(PythonOutput report, string originalCode) : base(originalCode, "python")
    {
        _report = report;
    }

    protected override Task<string> GenerateSummaryAsync()
    {
        _report.Summary.MessageCount = _report.Messages.Length;
        return RazorTemplateEngine.RenderAsync("~/Views/Shared/Summary.cshtml", _report.Messages.Length);
    }

    protected override async Task<string> GenerateErrorReportAsync()
    {
        var lines = OriginalCode.Split("\n");

        foreach (var message in _report.Messages)
        {
            if (!message.Location.Line.HasValue)
                continue;

            var index = message.Location.Line.Value - 1;
            var text = "";

            if (index > 0)
                text += lines[index - 1] + "\n\n";

            text += "# " + message.Message + "\n";

            text += lines[index];
            message.CommentedCode = text;
        }

        return await RazorTemplateEngine.RenderAsync("~/Views/Python/PythonErrors.cshtml", _report.Messages);
    }

    protected async Task<string> ExecuteAnalysisTool(string filePath)
    {
        Process toolProcess = new();
        string profilePath = Path.Join(IStorageHandler.ToolsProfilePath, Language, "ProspectorProfile.yml");

        ProcessStartInfo startInfo = new()
        {
            FileName = "prospector",
            Arguments = $"--profile-path \"{profilePath}\" --output-format json \"{filePath}\"",
            RedirectStandardOutput = true
        };

        toolProcess.StartInfo = startInfo;
        toolProcess.Start();
        await toolProcess.WaitForExitAsync();
        return await toolProcess.StandardOutput.ReadToEndAsync();
    }
}