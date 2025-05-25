using BookRenderer.Core.Models;

namespace BookRenderer.Core.Services;

public interface IMarkdownService
{
    Task<string> RenderMarkdownAsync(string markdown, string bookId);
    Task<string> RenderChapterAsync(string bookId, string chapterId);
    Task<TableOfContents> GenerateTableOfContentsAsync(string markdown);
    Task<List<CodeBlock>> ExtractCodeBlocksAsync(string markdown);
}

public interface IFileSystemService
{
    Task<string> ReadFileAsync(string filePath);
    Task WriteFileAsync(string filePath, string content);
    Task<bool> FileExistsAsync(string filePath);
    Task<bool> DirectoryExistsAsync(string directoryPath);
    Task CreateDirectoryAsync(string directoryPath);
    Task DeleteFileAsync(string filePath);
    Task DeleteDirectoryAsync(string directoryPath);
    Task<IEnumerable<string>> GetFilesAsync(string directoryPath, string pattern = "*");
    Task<IEnumerable<string>> GetDirectoriesAsync(string directoryPath);
}

public interface ICodeExecutionService
{
    Task<CodeExecutionResult> ExecuteCodeAsync(string code, string language, int timeoutSeconds = 30);
    bool IsLanguageSupported(string language);
    Task<bool> ValidateCodeAsync(string code, string language);
}

public class TableOfContents
{
    public List<TocItem> Items { get; set; } = new();
}

public class TocItem
{
    public string Title { get; set; } = string.Empty;
    public string Anchor { get; set; } = string.Empty;
    public int Level { get; set; }
    public List<TocItem> Children { get; set; } = new();
}

public class CodeBlock
{
    public string Language { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public bool IsExecutable { get; set; }
    public string Id { get; set; } = string.Empty;
}

public class CodeExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
}
