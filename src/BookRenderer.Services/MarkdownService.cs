using BookRenderer.Core.Services;
using Markdig;
using Markdig.Extensions.Mathematics;
using System.Text.RegularExpressions;

namespace BookRenderer.Services;

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly IFileSystemService _fileSystemService;

    public MarkdownService(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseMathematics()
            .Build();
    }

    public async Task<string> RenderMarkdownAsync(string markdown, string bookId)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        // Process relative image paths
        markdown = await ProcessImagePathsAsync(markdown, bookId);
        
        // Render to HTML
        var html = Markdown.ToHtml(markdown, _pipeline);
        
        // Post-process for code blocks
        html = ProcessCodeBlocks(html);
        
        return html;
    }

    public async Task<string> RenderChapterAsync(string bookId, string chapterId)
    {
        var chapterPath = Path.Combine("Data", "Books", bookId, "chapters", $"{chapterId}.md");
        
        if (!await _fileSystemService.FileExistsAsync(chapterPath))
            return "<p>Chapter not found.</p>";
            
        var content = await _fileSystemService.ReadFileAsync(chapterPath);
        return await RenderMarkdownAsync(content, bookId);
    }

    public Task<TableOfContents> GenerateTableOfContentsAsync(string markdown)
    {
        var toc = new TableOfContents();
        var lines = markdown.Split('\n');
        var stack = new Stack<TocItem>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#'))
            {
                var level = 0;
                while (level < trimmed.Length && trimmed[level] == '#')
                    level++;

                if (level <= 6) // Valid heading levels
                {
                    var title = trimmed.Substring(level).Trim();
                    var anchor = GenerateAnchor(title);
                    
                    var item = new TocItem
                    {
                        Title = title,
                        Anchor = anchor,
                        Level = level
                    };

                    // Handle nesting
                    while (stack.Count > 0 && stack.Peek().Level >= level)
                        stack.Pop();

                    if (stack.Count > 0)
                        stack.Peek().Children.Add(item);
                    else
                        toc.Items.Add(item);

                    stack.Push(item);
                }
            }
        }

        return Task.FromResult(toc);
    }

    public Task<List<CodeBlock>> ExtractCodeBlocksAsync(string markdown)
    {
        var codeBlocks = new List<CodeBlock>();
        var pattern = @"```(\w+)?\s*\n(.*?)\n```";
        var matches = Regex.Matches(markdown, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var language = match.Groups[1].Value.ToLowerInvariant();
            var code = match.Groups[2].Value;
            
            // Calculate line number
            var textBeforeMatch = markdown.Substring(0, match.Index);
            var lineNumber = textBeforeMatch.Split('\n').Length;

            var codeBlock = new CodeBlock
            {
                Id = $"code-block-{i}",
                Language = language,
                Code = code,
                LineNumber = lineNumber,
                IsExecutable = IsExecutableLanguage(language)
            };

            codeBlocks.Add(codeBlock);
        }

        return Task.FromResult(codeBlocks);
    }

    private async Task<string> ProcessImagePathsAsync(string markdown, string bookId)
    {
        // Convert relative image paths to absolute paths
        var pattern = @"!\[([^\]]*)\]\(([^)]+)\)";
        return Regex.Replace(markdown, pattern, match =>
        {
            var altText = match.Groups[1].Value;
            var imagePath = match.Groups[2].Value;
            
            // If it's already an absolute URL, leave it as is
            if (imagePath.StartsWith("http") || imagePath.StartsWith("/"))
                return match.Value;
                
            // Convert to absolute path
            var absolutePath = $"/api/books/{bookId}/assets/{imagePath}";
            return $"![{altText}]({absolutePath})";
        });
    }    private string ProcessCodeBlocks(string html)
    {
        // Add executable code block markers and handle Mermaid diagrams
        var pattern = @"<pre><code class=""language-(\w+)"">(.*?)</code></pre>";
        return Regex.Replace(html, pattern, match =>
        {
            var language = match.Groups[1].Value;
            var code = match.Groups[2].Value;
            
            // Handle Mermaid diagrams
            if (language.ToLowerInvariant() == "mermaid")
            {
                return $@"<div class=""mermaid-diagram"">
                    <div class=""mermaid"">{System.Net.WebUtility.HtmlDecode(code)}</div>
                </div>";
            }
            
            if (IsExecutableLanguage(language))
            {
                return $@"<div class=""executable-code-block"" data-language=""{language}"">
                    <div class=""code-header"">
                        <span class=""language"">{language}</span>
                        <button class=""execute-btn"" onclick=""executeCode(this)"">Run</button>
                    </div>
                    <pre><code class=""language-{language}"">{code}</code></pre>
                    <div class=""code-output"" style=""display: none;""></div>
                </div>";
            }
            
            return match.Value;
        }, RegexOptions.Singleline);
    }

    private string GenerateAnchor(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "");
    }

    private bool IsExecutableLanguage(string language)
    {
        var executableLanguages = new[] { "javascript", "js", "python", "py", "csharp", "cs" };
        return executableLanguages.Contains(language.ToLowerInvariant());
    }
}
