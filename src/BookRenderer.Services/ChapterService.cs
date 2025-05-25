using BookRenderer.Core.Models;
using BookRenderer.Core.Services;
using System.Text.Json;

namespace BookRenderer.Services;

public enum ChapterOperation
{
    Add,
    Update,
    Delete
}

public class ChapterService : IChapterService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IGitService _gitService;
    private readonly IBookService _bookService;
    private readonly string _dataPath;    public ChapterService(IFileSystemService fileSystemService, IGitService gitService, IBookService bookService)
    {
        _fileSystemService = fileSystemService;
        _gitService = gitService;
        _bookService = bookService;
        
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
    }    public async Task<Chapter> CreateChapterAsync(Chapter chapter)
    {
        Console.WriteLine($"[DEBUG] CreateChapterAsync - Input: BookId={chapter.BookId}, Title={chapter.Title}, Order={chapter.Order}");
        
        chapter.Id = GenerateChapterId(chapter.Title);
        chapter.FileName = $"{chapter.Order:D2}-{chapter.Id}.md";
        chapter.CreatedAt = DateTime.UtcNow;
        chapter.UpdatedAt = DateTime.UtcNow;

        Console.WriteLine($"[DEBUG] CreateChapterAsync - Generated: Id={chapter.Id}, FileName={chapter.FileName}");

        var chapterPath = Path.Combine(_dataPath, chapter.BookId, "chapters", chapter.FileName);
        Console.WriteLine($"[DEBUG] CreateChapterAsync - Chapter path: {chapterPath}");
        
        // Create initial content if empty
        if (string.IsNullOrEmpty(chapter.Content))
        {
            chapter.Content = $"# {chapter.Title}\n\nThis chapter is under construction.";
        }

        Console.WriteLine($"[DEBUG] CreateChapterAsync - Writing file to: {chapterPath}");
        await _fileSystemService.WriteFileAsync(chapterPath, chapter.Content);
        Console.WriteLine($"[DEBUG] CreateChapterAsync - File written successfully");

        // Update the book.json metadata to include this new chapter
        await UpdateBookMetadataWithChapterAsync(chapter.BookId, chapter, ChapterOperation.Add);

        // Commit the new chapter
        var bookPath = Path.Combine(_dataPath, chapter.BookId);
        Console.WriteLine($"[DEBUG] CreateChapterAsync - Committing to git at: {bookPath}");
        await _gitService.CommitChangesAsync(bookPath, $"Added new chapter: {chapter.Title}");
        Console.WriteLine($"[DEBUG] CreateChapterAsync - Git commit successful");

        return chapter;
    }    public async Task<Chapter> UpdateChapterAsync(Chapter chapter)
    {
        Console.WriteLine($"[DEBUG] UpdateChapterAsync - Input: BookId={chapter.BookId}, Id={chapter.Id}, Title={chapter.Title}, FileName={chapter.FileName}");
        
        chapter.UpdatedAt = DateTime.UtcNow;
        
        // Ensure we have a valid filename
        if (string.IsNullOrEmpty(chapter.FileName))
        {
            Console.WriteLine("[DEBUG] UpdateChapterAsync - FileName is empty, generating from Order and Id");
            chapter.FileName = $"{chapter.Order:D2}-{chapter.Id}.md";
        }
        
        var chapterPath = Path.Combine(_dataPath, chapter.BookId, "chapters", chapter.FileName);
        Console.WriteLine($"[DEBUG] UpdateChapterAsync - Chapter path: {chapterPath}");
        
        // Verify the file exists before trying to update
        if (!await _fileSystemService.FileExistsAsync(chapterPath))
        {
            Console.WriteLine($"[DEBUG] UpdateChapterAsync - File does not exist: {chapterPath}");
            throw new FileNotFoundException($"Chapter file not found: {chapterPath}");
        }
        
        Console.WriteLine($"[DEBUG] UpdateChapterAsync - Writing file to: {chapterPath}");
        await _fileSystemService.WriteFileAsync(chapterPath, chapter.Content);
        Console.WriteLine($"[DEBUG] UpdateChapterAsync - File written successfully");

        // Update the book.json metadata to reflect changes
        await UpdateBookMetadataWithChapterAsync(chapter.BookId, chapter, ChapterOperation.Update);

        // Commit the changes
        var bookPath = Path.Combine(_dataPath, chapter.BookId);
        Console.WriteLine($"[DEBUG] UpdateChapterAsync - Committing to git at: {bookPath}");
        await _gitService.CommitChangesAsync(bookPath, $"Updated chapter: {chapter.Title} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"[DEBUG] UpdateChapterAsync - Git commit successful");

        return chapter;
    }    public async Task<bool> DeleteChapterAsync(string bookId, string chapterId)
    {
        var chapters = await GetChaptersAsync(bookId);
        var chapter = chapters.FirstOrDefault(c => c.Id == chapterId);
        
        if (chapter == null)
            return false;

        var chapterPath = Path.Combine(_dataPath, bookId, "chapters", chapter.FileName);
        await _fileSystemService.DeleteFileAsync(chapterPath);

        // Update the book.json metadata to remove this chapter
        await UpdateBookMetadataWithChapterAsync(bookId, chapter, ChapterOperation.Delete);

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

    private async Task UpdateBookMetadataWithChapterAsync(string bookId, Chapter chapter, ChapterOperation operation)
    {
        Console.WriteLine($"[DEBUG] UpdateBookMetadataWithChapterAsync - BookId: {bookId}, Chapter: {chapter.Title}, Operation: {operation}");
        
        try
        {
            // Load the current book
            var book = await _bookService.GetBookByIdAsync(bookId);
            if (book == null)
            {
                Console.WriteLine($"[DEBUG] UpdateBookMetadataWithChapterAsync - Book not found: {bookId}");
                return;
            }

            // Update the chapters list based on the operation
            var chapters = book.Chapters?.ToList() ?? new List<Chapter>();
            
            switch (operation)
            {
                case ChapterOperation.Add:
                    // Add the new chapter (without content to keep JSON size reasonable)
                    var chapterMetadata = new Chapter
                    {
                        Id = chapter.Id,
                        BookId = chapter.BookId,
                        Title = chapter.Title,
                        FileName = chapter.FileName,
                        Order = chapter.Order,
                        Content = "", // Don't store content in book.json
                        IsPublished = chapter.IsPublished,
                        CreatedAt = chapter.CreatedAt,
                        UpdatedAt = chapter.UpdatedAt
                    };
                    chapters.Add(chapterMetadata);
                    break;
                    
                case ChapterOperation.Update:
                    // Find and update the existing chapter
                    var existingChapter = chapters.FirstOrDefault(c => c.Id == chapter.Id);
                    if (existingChapter != null)
                    {
                        existingChapter.Title = chapter.Title;
                        existingChapter.FileName = chapter.FileName;
                        existingChapter.Order = chapter.Order;
                        existingChapter.IsPublished = chapter.IsPublished;
                        existingChapter.UpdatedAt = chapter.UpdatedAt;
                    }
                    break;
                    
                case ChapterOperation.Delete:
                    // Remove the chapter
                    chapters.RemoveAll(c => c.Id == chapter.Id);
                    break;
            }

            // Sort chapters by order and update the book
            book.Chapters = chapters.OrderBy(c => c.Order).ToList();
            book.UpdatedAt = DateTime.UtcNow;

            // Save the updated book metadata
            await _bookService.UpdateBookAsync(book);
            
            Console.WriteLine($"[DEBUG] UpdateBookMetadataWithChapterAsync - Successfully updated book metadata");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] UpdateBookMetadataWithChapterAsync - Error: {ex.Message}");
            // Don't throw - we don't want chapter operations to fail because of metadata issues
        }
    }
}
