using Markdig;

namespace Core.Renderer;

public static class Renderer
{
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
    
    public static string RenderMarkdownToHtml(this string markdownText)
    {
        
        if (!markdownText.Contains("```"))
            return Markdown.ToHtml(markdownText, _pipeline);
        
        var output = string.Empty;
        int codeBlockStart;
        
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