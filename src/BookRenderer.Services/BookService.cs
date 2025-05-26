using BookRenderer.Core.Models;
using BookRenderer.Core.Services;
using System.Text.Json;

namespace BookRenderer.Services;

public class BookService : IBookService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IGitService _gitService;
    private readonly string _dataPath;

    public BookService(IFileSystemService fileSystemService, IGitService gitService, string? dataPath = null)
    {
        _fileSystemService = fileSystemService;
        _gitService = gitService;
        
        // If dataPath is provided, use it. Otherwise, try to find the Data folder relative to the solution
        if (!string.IsNullOrEmpty(dataPath))
        {
            _dataPath = Path.Combine(dataPath, "Books");
        }
        else
        {
            // Navigate up from the app directory to find the Data folder
            var currentDir = AppContext.BaseDirectory;
            var solutionDir = FindSolutionDirectory(currentDir);
            _dataPath = Path.Combine(solutionDir ?? currentDir, "Data", "Books");
        }
    }

    private string? FindSolutionDirectory(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            // Look for the Data directory or solution file
            if (Directory.Exists(Path.Combine(dir.FullName, "Data")) || 
                dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    public async Task<IEnumerable<Book>> GetAllBooksAsync()
    {
        var books = new List<Book>();
        
        if (!await _fileSystemService.DirectoryExistsAsync(_dataPath))
            return books;

        var bookDirectories = await _fileSystemService.GetDirectoriesAsync(_dataPath);
        
        foreach (var bookDir in bookDirectories)
        {
            var book = await LoadBookFromDirectoryAsync(bookDir);
            if (book != null)
                books.Add(book);
        }

        return books.OrderBy(b => b.Title);
    }

    public async Task<IEnumerable<Book>> GetBooksForUserAsync(string userId)
    {
        var allBooks = await GetAllBooksAsync();
        
        return allBooks.Where(book => 
            book.IsPublic || 
            book.AllowedUsers.Contains(userId, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<Book?> GetBookByIdAsync(string bookId)
    {
        var bookPath = Path.Combine(_dataPath, bookId);
        
        if (!await _fileSystemService.DirectoryExistsAsync(bookPath))
            return null;

        return await LoadBookFromDirectoryAsync(bookPath);
    }

    public async Task<Book> CreateBookAsync(Book book)
    {
        book.Id = GenerateBookId(book.Title);
        book.CreatedAt = DateTime.UtcNow;
        book.UpdatedAt = DateTime.UtcNow;

        var bookPath = Path.Combine(_dataPath, book.Id);
        await _fileSystemService.CreateDirectoryAsync(bookPath);
        
        // Create subdirectories
        await _fileSystemService.CreateDirectoryAsync(Path.Combine(bookPath, "chapters"));
        await _fileSystemService.CreateDirectoryAsync(Path.Combine(bookPath, "assets"));

        // Initialize Git repository
        await _gitService.InitializeRepositoryAsync(bookPath);

        // Save book metadata
        await SaveBookMetadataAsync(book);

        // Commit initial state
        await _gitService.CommitChangesAsync(bookPath, "Initial book creation");

        return book;
    }

    public async Task<Book> UpdateBookAsync(Book book)
    {
        book.UpdatedAt = DateTime.UtcNow;
        await SaveBookMetadataAsync(book);

        var bookPath = Path.Combine(_dataPath, book.Id);
        await _gitService.CommitChangesAsync(bookPath, $"Updated book metadata - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

        return book;
    }

    public async Task<bool> DeleteBookAsync(string bookId)
    {
        var bookPath = Path.Combine(_dataPath, bookId);
        
        if (!await _fileSystemService.DirectoryExistsAsync(bookPath))
            return false;

        await _fileSystemService.DeleteDirectoryAsync(bookPath);
        return true;
    }

    public async Task<bool> UserHasAccessToBookAsync(string userId, string bookId)
    {
        var book = await GetBookByIdAsync(bookId);
        
        if (book == null)
            return false;

        return book.IsPublic || book.AllowedUsers.Contains(userId, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Book?> LoadBookFromDirectoryAsync(string bookPath)
    {
        var bookMetadataPath = Path.Combine(bookPath, "book.json");
        
        if (!await _fileSystemService.FileExistsAsync(bookMetadataPath))
            return null;

        try
        {
            var json = await _fileSystemService.ReadFileAsync(bookMetadataPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            Console.WriteLine(json); // Debugging line to check JSON content
            var book = JsonSerializer.Deserialize<Book>(json, options);
            
            if (book != null)
            {
                // Only scan the filesystem for chapters if none are defined in metadata
                if (book.Chapters == null || book.Chapters.Count == 0)
                {
                    await LoadChaptersAsync(book, bookPath);
                }

                // Ensure each chapter has the correct BookId and is ordered
                if (book.Chapters != null)
                {
                    foreach (var ch in book.Chapters)
                    {
                        if (string.IsNullOrEmpty(ch.BookId))
                            ch.BookId = book.Id;
                    }

                    book.Chapters = book.Chapters.OrderBy(c => c.Order).ToList();
                }
            }

            return book;
        }
        catch
        {
            return null;
        }
    }    private async Task LoadChaptersAsync(Book book, string bookPath)
    {
        var chaptersPath = Path.Combine(bookPath, "chapters");
        
        if (!await _fileSystemService.DirectoryExistsAsync(chaptersPath))
            return;

        // Start with existing chapters from metadata (if any)
        var chapters = book.Chapters?.ToList() ?? new List<Chapter>();
        
        // Get all .md files in the chapters directory
        var chapterFiles = await _fileSystemService.GetFilesAsync(chaptersPath, "*.md");
        
        foreach (var chapterFile in chapterFiles)
        {
            var fileName = Path.GetFileName(chapterFile);
            var chapterId = Path.GetFileNameWithoutExtension(fileName);
            
            // Check if this chapter already exists in metadata
            var existingChapter = chapters.FirstOrDefault(c => 
                c.Id == chapterId || 
                c.FileName == fileName ||
                (!string.IsNullOrEmpty(c.FileName) && Path.GetFileNameWithoutExtension(c.FileName) == chapterId));
            
            if (existingChapter != null)
            {
                // Update existing chapter with filesystem info, but preserve metadata
                existingChapter.FileName = fileName;
                // Only update BookId if it's empty
                if (string.IsNullOrEmpty(existingChapter.BookId))
                {
                    existingChapter.BookId = book.Id;
                }
                Console.WriteLine($"[LoadChaptersAsync] Preserved metadata chapter: {existingChapter.Id} - '{existingChapter.Title}'");
            }
            else
            {
                // This is a new chapter file not in metadata - create from filesystem
                var order = ExtractOrderFromFileName(fileName);
                
                var chapter = new Chapter
                {
                    Id = chapterId,
                    BookId = book.Id,
                    FileName = fileName,
                    Order = order,
                    Title = ExtractTitleFromFileName(fileName)
                };

                chapters.Add(chapter);
                Console.WriteLine($"[LoadChaptersAsync] Added new filesystem chapter: {chapter.Id} - '{chapter.Title}'");
            }
        }

        // Remove any metadata chapters that no longer have corresponding .md files
        var validChapters = chapters.Where(c => 
            !string.IsNullOrEmpty(c.FileName) && 
            chapterFiles.Any(f => Path.GetFileName(f) == c.FileName)).ToList();

        book.Chapters = validChapters.OrderBy(c => c.Order).ToList();
        
        Console.WriteLine($"[LoadChaptersAsync] Final chapter count: {book.Chapters.Count}");
        foreach (var chapter in book.Chapters)
        {
            Console.WriteLine($"[LoadChaptersAsync] Final chapter: {chapter.Id} - '{chapter.Title}' (Order: {chapter.Order})");
        }
    }

    private async Task SaveBookMetadataAsync(Book book)
    {
        var bookPath = Path.Combine(_dataPath, book.Id);
        var bookMetadataPath = Path.Combine(bookPath, "book.json");
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(book, options);
        await _fileSystemService.WriteFileAsync(bookMetadataPath, json);
    }

    private string GenerateBookId(string title)
    {
        var id = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "");
            
        // Add timestamp to ensure uniqueness
        return $"{id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private int ExtractOrderFromFileName(string fileName)
    {
        // Try to extract number from beginning of filename
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"^(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int order))
            return order;

        return 999; // Default to high number for unordered chapters
    }

    private string ExtractTitleFromFileName(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        
        // Remove leading numbers and separators
        var title = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"^\d+[-_]?", "");
        
        // Replace dashes and underscores with spaces and title case
        title = title.Replace("-", " ").Replace("_", " ");
        
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title);
    }
}
