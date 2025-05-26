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
        Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - BookId: {bookId}");
        var book = await _bookService.GetBookByIdAsync(bookId);

        if (book == null || book.Chapters == null || !book.Chapters.Any())
        {
            Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Book metadata or chapters not found for BookId: {bookId}");
            return Enumerable.Empty<Chapter>();
        }

        var chapters = new List<Chapter>();
        foreach (var chapterMetadata in book.Chapters.OrderBy(c => c.Order))
        {
            if (string.IsNullOrEmpty(chapterMetadata.FileName))
            {
                Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Skipping chapter with missing FileName: {chapterMetadata.Id} - {chapterMetadata.Title}");
                continue;
            }

            var chapterPath = Path.Combine(_dataPath, bookId, "chapters", chapterMetadata.FileName);
            Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Loading content for: {chapterPath}");
            string content = string.Empty;
            DateTime fileLastWriteTimeUtc = chapterMetadata.UpdatedAt; // Default to metadata update time

            if (await _fileSystemService.FileExistsAsync(chapterPath))
            {
                content = await _fileSystemService.ReadFileAsync(chapterPath);
                // Optionally, get actual file modification time if relevant
                // var fileInfo = new FileInfo(chapterPath); // Requires System.IO, ensure _fileSystemService can provide this
                // fileLastWriteTimeUtc = fileInfo.LastWriteTimeUtc; 
            }
            else
            {
                Console.WriteLine($"[WARNING] ChapterService.GetChaptersAsync - Chapter file not found, but listed in metadata: {chapterPath}. Content will be empty.");
                // Decide if this chapter should be skipped or returned with empty content
            }

            chapters.Add(new Chapter
            {
                Id = chapterMetadata.Id,
                BookId = bookId, // or chapterMetadata.BookId if it's guaranteed to be set
                Title = chapterMetadata.Title, // Use title from metadata
                FileName = chapterMetadata.FileName,
                Order = chapterMetadata.Order,
                Content = content,
                IsPublished = chapterMetadata.IsPublished,
                CreatedAt = chapterMetadata.CreatedAt,
                // Use metadata UpdatedAt, as it reflects changes to title/order.
                // Content changes are saved with chapter.UpdatedAt in UpdateChapterAsync.
                UpdatedAt = chapterMetadata.UpdatedAt 
            });
        }

        Console.WriteLine($"[DEBUG] ChapterService.GetChaptersAsync - Returning {chapters.Count} chapters for BookId: {bookId}");
        return chapters; // Already ordered by Order from book.Chapters query
    }

    public async Task<Chapter?> GetChapterAsync(string bookId, string chapterId)
    {
        Console.WriteLine($"[DEBUG] ChapterService.GetChapterAsync - BookId: {bookId}, ChapterId: {chapterId}");
        var book = await _bookService.GetBookByIdAsync(bookId);

        if (book == null || book.Chapters == null)
        {
            Console.WriteLine($"[DEBUG] ChapterService.GetChapterAsync - Book metadata not found for BookId: {bookId}");
            return null;
        }

        var chapterMetadata = book.Chapters.FirstOrDefault(c => c.Id == chapterId);

        if (chapterMetadata == null)
        {
            Console.WriteLine($"[DEBUG] ChapterService.GetChapterAsync - Chapter metadata not found for ChapterId: {chapterId} in BookId: {bookId}");
            return null;
        }

        if (string.IsNullOrEmpty(chapterMetadata.FileName))
        {
            Console.WriteLine($"[DEBUG] ChapterService.GetChapterAsync - Chapter metadata found, but FileName is missing for ChapterId: {chapterId}");
            // Return a chapter object without content, or handle as an error
            return new Chapter 
            {
                 Id = chapterMetadata.Id,
                BookId = bookId,
                Title = chapterMetadata.Title, // Use title from metadata
                FileName = chapterMetadata.FileName,
                Order = chapterMetadata.Order,
                Content = "Error: Chapter file name is missing in metadata.",
                IsPublished = chapterMetadata.IsPublished,
                CreatedAt = chapterMetadata.CreatedAt,
                UpdatedAt = chapterMetadata.UpdatedAt
            };
        }

        var chapterPath = Path.Combine(_dataPath, bookId, "chapters", chapterMetadata.FileName);
        Console.WriteLine($"[DEBUG] ChapterService.GetChapterAsync - Loading content from: {chapterPath}");
        string content = string.Empty;
        DateTime fileLastWriteTimeUtc = chapterMetadata.UpdatedAt;

        if (await _fileSystemService.FileExistsAsync(chapterPath))
        {
            content = await _fileSystemService.ReadFileAsync(chapterPath);
            // var fileInfo = new FileInfo(chapterPath);
            // fileLastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
        }
        else
        {
            Console.WriteLine($"[WARNING] ChapterService.GetChapterAsync - Chapter file not found: {chapterPath}. Content will be empty.");
            content = $"Error: Chapter file '{chapterMetadata.FileName}' not found."; // Or return null, or throw
        }

        return new Chapter
        {
            Id = chapterMetadata.Id,
            BookId = bookId, 
            Title = chapterMetadata.Title, // Use title from metadata
            FileName = chapterMetadata.FileName,
            Order = chapterMetadata.Order,
            Content = content,
            IsPublished = chapterMetadata.IsPublished,
            CreatedAt = chapterMetadata.CreatedAt,
            UpdatedAt = chapterMetadata.UpdatedAt // Or fileLastWriteTimeUtc if more appropriate for content changes
        };
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
        // This method is now less critical for GetChaptersAsync and GetChapterAsync 
        // as they fetch metadata first. It could be used for other purposes or deprecated.
        // For now, let's keep its original functionality if called directly,
        // but it won't be used for title determination by the main methods.
        try
        {
            var content = await _fileSystemService.ReadFileAsync(chapterPath);
            var fileName = Path.GetFileName(chapterPath);
            
            // The chapterId should ideally come from metadata. If this method is used standalone,
            // this is a fallback.
            var chapterId = Path.GetFileNameWithoutExtension(fileName).Split('-').LastOrDefault() ?? Path.GetFileNameWithoutExtension(fileName);
            
            var order = ExtractOrderFromFileName(fileName);
            // Title extraction here is now a fallback, metadata is source of truth.
            var title = ExtractTitleFromContent(content) ?? ExtractTitleFromFileName(fileName);

            // FileInfo might not be directly available via IFileSystemService,
            // so using DateTime.MinValue or fetching from service if possible.
            // For simplicity, we'll assume metadata provides these if needed.
            // If this method is to be fully standalone, it needs a way to get CreatedAt/UpdatedAt.

            return new Chapter
            {
                Id = chapterId, // Fallback Id
                BookId = bookId,
                Title = title, // Fallback title
                FileName = fileName,
                Order = order, // Fallback order
                Content = content,
                CreatedAt = DateTime.UtcNow, // Placeholder, ideally from metadata or file system
                UpdatedAt = DateTime.UtcNow  // Placeholder
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] LoadChapterFromFileAsync - Failed to load {chapterPath}: {ex.Message}");
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
