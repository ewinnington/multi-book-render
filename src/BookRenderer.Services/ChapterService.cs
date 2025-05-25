using BookRenderer.Core.Models;
using BookRenderer.Core.Services;

namespace BookRenderer.Services;

public class ChapterService : IChapterService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IGitService _gitService;
    private readonly string _dataPath;    public ChapterService(IFileSystemService fileSystemService, IGitService gitService)
    {
        _fileSystemService = fileSystemService;
        _gitService = gitService;
        
        // Use the same path resolution logic as BookService
        var currentDir = AppContext.BaseDirectory;
        var solutionDir = FindSolutionDirectory(currentDir);
        _dataPath = Path.Combine(solutionDir ?? currentDir, "Data", "Books");
        
        Console.WriteLine($"[DEBUG] ChapterService constructor - Data path: {_dataPath}");
        Console.WriteLine($"[DEBUG] ChapterService constructor - AppContext.BaseDirectory: {AppContext.BaseDirectory}");
    }public async Task<IEnumerable<Chapter>> GetChaptersAsync(string bookId)
    {
        var chaptersPath = Path.Combine(_dataPath, bookId, "chapters");
        Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Looking for chapters in: {chaptersPath}");
        
        if (!await _fileSystemService.DirectoryExistsAsync(chaptersPath))
        {
            Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Directory does not exist: {chaptersPath}");
            return Enumerable.Empty<Chapter>();
        }

        var chapterFiles = await _fileSystemService.GetFilesAsync(chaptersPath, "*.md");
        Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Found {chapterFiles.Count()} .md files");
        
        var chapters = new List<Chapter>();

        foreach (var chapterFile in chapterFiles)
        {
            Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Processing file: {chapterFile}");
            var chapter = await LoadChapterFromFileAsync(chapterFile, bookId);
            if (chapter != null)
            {
                Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Loaded chapter: {chapter.Id}, Order: {chapter.Order}");
                chapters.Add(chapter);
            }
            else
            {
                Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Failed to load chapter from: {chapterFile}");
            }
        }

        Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Returning {chapters.Count} chapters");
        return chapters.OrderBy(c => c.Order);
    }public async Task<Chapter?> GetChapterAsync(string bookId, string chapterId)
    {
        var chapterPath = Path.Combine(_dataPath, bookId, "chapters", $"{chapterId}.md");
        Console.WriteLine($"[DEBUG] ChapterService.GetChapterAsync - Looking for chapter at: {chapterPath}");
        
        if (!await _fileSystemService.FileExistsAsync(chapterPath))
        {
            Console.WriteLine($"[DEBUG] Chapter file not found: {chapterPath}");
            return null;
        }

        var result = await LoadChapterFromFileAsync(chapterPath, bookId);
        Console.WriteLine($"[DEBUG] Loaded chapter: {result?.Id} with content length: {result?.Content?.Length ?? 0}");
        return result;
    }

    public async Task<Chapter?> GetChapterByOrderAsync(string bookId, int order)
    {
        var chapters = await GetChaptersAsync(bookId);
        return chapters.FirstOrDefault(c => c.Order == order);
    }

    public async Task<Chapter> CreateChapterAsync(string bookId, Chapter chapter)
    {
        chapter.Id = GenerateChapterId(chapter.Title);
        chapter.FileName = $"{chapter.Order:D2}-{chapter.Id}.md";
        chapter.CreatedAt = DateTime.UtcNow;
        chapter.UpdatedAt = DateTime.UtcNow;

        var chapterPath = Path.Combine(_dataPath, bookId, "chapters", chapter.FileName);
        
        // Create initial content if empty
        if (string.IsNullOrEmpty(chapter.Content))
        {
            chapter.Content = $"# {chapter.Title}\n\nThis chapter is under construction.";
        }

        await _fileSystemService.WriteFileAsync(chapterPath, chapter.Content);

        // Commit the new chapter
        var bookPath = Path.Combine(_dataPath, bookId);
        await _gitService.CommitChangesAsync(bookPath, $"Added new chapter: {chapter.Title}");

        return chapter;
    }

    public async Task<Chapter> UpdateChapterAsync(string bookId, Chapter chapter)
    {
        chapter.UpdatedAt = DateTime.UtcNow;
        
        var chapterPath = Path.Combine(_dataPath, bookId, "chapters", chapter.FileName);
        await _fileSystemService.WriteFileAsync(chapterPath, chapter.Content);

        // Commit the changes
        var bookPath = Path.Combine(_dataPath, bookId);
        await _gitService.CommitChangesAsync(bookPath, $"Updated chapter: {chapter.Title} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

        return chapter;
    }

    public async Task<bool> DeleteChapterAsync(string bookId, string chapterId)
    {
        var chapters = await GetChaptersAsync(bookId);
        var chapter = chapters.FirstOrDefault(c => c.Id == chapterId);
        
        if (chapter == null)
            return false;

        var chapterPath = Path.Combine(_dataPath, bookId, "chapters", chapter.FileName);
        await _fileSystemService.DeleteFileAsync(chapterPath);

        // Commit the deletion
        var bookPath = Path.Combine(_dataPath, bookId);
        await _gitService.CommitChangesAsync(bookPath, $"Deleted chapter: {chapter.Title}");

        return true;
    }

    public async Task<bool> ReorderChaptersAsync(string bookId, List<string> chapterIds)
    {
        try
        {
            var chapters = await GetChaptersAsync(bookId);
            var chapterDict = chapters.ToDictionary(c => c.Id);

            for (int i = 0; i < chapterIds.Count; i++)
            {
                if (chapterDict.TryGetValue(chapterIds[i], out var chapter))
                {
                    var newOrder = i + 1;
                    if (chapter.Order != newOrder)
                    {
                        // Generate new filename with correct order
                        var newFileName = $"{newOrder:D2}-{chapter.Id}.md";
                        var oldPath = Path.Combine(_dataPath, bookId, "chapters", chapter.FileName);
                        var newPath = Path.Combine(_dataPath, bookId, "chapters", newFileName);

                        // Rename the file
                        if (await _fileSystemService.FileExistsAsync(oldPath))
                        {
                            var content = await _fileSystemService.ReadFileAsync(oldPath);
                            await _fileSystemService.WriteFileAsync(newPath, content);
                            await _fileSystemService.DeleteFileAsync(oldPath);
                        }

                        chapter.Order = newOrder;
                        chapter.FileName = newFileName;
                    }
                }
            }

            // Commit the reordering
            var bookPath = Path.Combine(_dataPath, bookId);
            await _gitService.CommitChangesAsync(bookPath, "Reordered chapters");

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<Chapter?> LoadChapterFromFileAsync(string chapterPath, string bookId)
    {
        try
        {
            var content = await _fileSystemService.ReadFileAsync(chapterPath);
            var fileName = Path.GetFileName(chapterPath);
            var chapterId = Path.GetFileNameWithoutExtension(fileName);
            
            // Extract order from filename
            var order = ExtractOrderFromFileName(fileName);
            
            // Extract title from content (first H1) or filename
            var title = ExtractTitleFromContent(content) ?? ExtractTitleFromFileName(fileName);

            var fileInfo = new FileInfo(chapterPath);

            return new Chapter
            {
                Id = chapterId,
                Title = title,
                FileName = fileName,
                Order = order,
                Content = content,
                CreatedAt = fileInfo.CreationTimeUtc,
                UpdatedAt = fileInfo.LastWriteTimeUtc
            };
        }
        catch
        {
            return null;
        }
    }

    private string GenerateChapterId(string title)
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

    private int ExtractOrderFromFileName(string fileName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"^(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int order))
            return order;

        return 999;
    }

    private string ExtractTitleFromFileName(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var title = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"^\d+[-_]?", "");
        title = title.Replace("-", " ").Replace("_", " ");
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title);
    }

    private string? ExtractTitleFromContent(string content)
    {
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# "))
            {
                return trimmed.Substring(2).Trim();
            }
        }        return null;
    }
    
    private string? FindSolutionDirectory(string startingPath)
    {
        var directory = new DirectoryInfo(startingPath);
        
        while (directory != null)
        {
            // Look for .sln file or src folder
            if (directory.GetFiles("*.sln").Any() || 
                directory.GetDirectories("src").Any())
            {
                return directory.FullName;
            }
            
            directory = directory.Parent;
        }
        
        return null;
    }
}
