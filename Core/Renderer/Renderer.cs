using Markdig;

namespace Core.Renderer;

/// <summary>
/// Helper class to convert Markdown into HTML using MarkDig nuget package
/// </summary>
public static class Renderer
{
    /// <summary>
    /// Pipeline that allows us to do various things in markdown!
    /// </summary>
    private static MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UsePipeTables()
        .UseBootstrap()
        .UseFooters()
        .UseCitations()
        .UseAutoIdentifiers()
        .UseAutoLinks()
        .UseMediaLinks()
        .UseListExtras()
        .UseCustomContainers()
        .UseEmojiAndSmiley().Build();
    
    /// <summary>
    /// Convert <paramref name="markdownText"/> into html
    /// </summary>
    /// <param name="markdownText">Text to convert</param>
    /// <returns>HTML version of <paramref name="markdownText"/></returns>
    public static string RenderMarkdownToHtml(this string markdownText)
    {
        
        if (!markdownText.Contains("```"))
            return Markdown.ToHtml(markdownText, _pipeline);
        
        var output = string.Empty;
        int codeBlockStart;
        
        /*
            We are utilizing HighlightJs for rendering codeblocks in our templates.
            The markdown renderer does not convert these for us unfortunately so
            we must parse it ourselves.
            
            ```language
            some awesome code goes in here
            ```
            
            Based on the above format we need to go by ``` as our markers.
            So long as there is a ``` in the file we have a codeblock!
         */
        
        while ((codeBlockStart = markdownText.IndexOf("```")) > 0)
        {
            // Get everything from before the code block up until this point
            output += markdownText[..codeBlockStart];
            
            // reduce the markdown scope
            markdownText = markdownText[codeBlockStart..];
            
            // get the language associated with this code block
            var langEnd = markdownText.IndexOf('\n');
            var language = markdownText[3..langEnd];
            markdownText = markdownText[(langEnd+1)..];
            
            // retrieve the code block ending
            var codeBlockEnd = markdownText.IndexOf("```");
            var contents = markdownText[..codeBlockEnd];
            output += $"<pre><code class=\"language-{language}\">{contents}</code></pre>";
            
            // reduce scope of markdown text
            markdownText = markdownText[(codeBlockEnd + 3)..];
        }

        output += markdownText;
        
        return Markdown.ToHtml(output,_pipeline);
    }
}