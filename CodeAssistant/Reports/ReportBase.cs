using Razor.Templating.Core;

namespace CodeAssistant.Reports;

public abstract class ReportBase
{
    /// <summary>
    /// Report Title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Compiled HTML contents
    /// </summary>
    public string Contents { get; set; }


    /// <summary>
    /// Type of language that shall be used for highlighting purposes
    /// </summary>
    public readonly string Language;

    /// <summary>
    /// Original code the user provided
    /// </summary>
    protected readonly string OriginalCode;

    protected ReportBase(string fileContents, string language)
    {
        Language = language;
        OriginalCode = fileContents;
    }

    /// <summary>
    /// Generates the HTML page that the user can view
    /// </summary>
    /// <returns></returns>
    public async Task<string> GenerateReportAsync()
    {
        Contents = await GenerateSummaryAsync() +
                   await GenerateErrorReportAsync() + Html.CreateCodeBlock(Language, OriginalCode);
        return await RazorTemplateEngine.RenderAsync("~/Views/Shared/Main.cshtml", this);
    }

    /// <summary>
    /// Delete <paramref name="filePath"/> to keep disk-space tidy
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    protected Task CleanupAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return Task.CompletedTask;

        File.Delete(filePath);

        return Task.CompletedTask;
    }
    
    protected virtual Task<string> ExecuteAnalysisTool() => Task.FromResult(string.Empty);

    /// <summary>
    /// Generate summary block that appears on top of the report
    /// </summary>
    /// <returns></returns>
    protected virtual Task<string> GenerateSummaryAsync() => Task.FromResult("<p>Default Summary</p>");
    
    /// <summary>
    /// Generate the error/suggestion messages
    /// </summary>
    /// <returns></returns>
    protected virtual Task<string> GenerateErrorReportAsync() => Task.FromResult("<p>No errors to report</p>");
}
