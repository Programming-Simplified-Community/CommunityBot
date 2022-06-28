using CodeAssistant.Reports;

namespace CodeAssistant.Services;

public class CodeAssistantService// : ICodeReviewService
{
    public async Task<ReportBase?> AnalyzeAsync(string codeContent)
    {
        
        return null;
    }

    public static ReadOnlySpan<char> ExtractContent(string content, out string language)
    {
        var span = content.AsSpan();

        if (!(span[..3].ToString() == "```" && span[^3..].ToString() == "```"))
        {
            language = string.Empty;
            return span;
        }

        var languageEnd = span.IndexOf("\r\n");

        // Perhaps no carriage return
        if (languageEnd == -1)
            languageEnd = span.IndexOf("\n");

        // well we definitely goofed then
        if (languageEnd == -1)
        {
            language = string.Empty;
            return span;
        }
        
        language = span[3..languageEnd].ToString();
        return span[languageEnd ..^3];
    }
}