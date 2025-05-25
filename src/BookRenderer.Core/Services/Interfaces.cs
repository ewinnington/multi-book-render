using BookRenderer.Core.Models;

namespace BookRenderer.Core.Services;

public interface IBookService
{
    Task<IEnumerable<Book>> GetAllBooksAsync();
    Task<IEnumerable<Book>> GetBooksForUserAsync(string userId);
    Task<Book?> GetBookByIdAsync(string bookId);
    Task<Book> CreateBookAsync(Book book);
    Task<Book> UpdateBookAsync(Book book);
    Task<bool> DeleteBookAsync(string bookId);
    Task<bool> UserHasAccessToBookAsync(string userId, string bookId);
}

public interface IChapterService
{
    Task<IEnumerable<Chapter>> GetChaptersAsync(string bookId);
    Task<Chapter?> GetChapterAsync(string bookId, string chapterId);
    Task<Chapter?> GetChapterByOrderAsync(string bookId, int order);
    Task<Chapter> CreateChapterAsync(Chapter chapter);
    Task<Chapter> UpdateChapterAsync(Chapter chapter);
    Task<bool> DeleteChapterAsync(string bookId, string chapterId);
    Task<bool> ReorderChaptersAsync(string bookId, List<string> chapterIds);
}

public interface IUserService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User> CreateUserAsync(User user, string password);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string userId);
    Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
    Task<IEnumerable<User>> GetAllUsersAsync();
}

public interface IGitService
{
    Task<bool> InitializeRepositoryAsync(string bookPath);
    Task<bool> CommitChangesAsync(string bookPath, string message);
    Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string bookPath);
    Task<GitStatus> GetStatusAsync(string bookPath);
}

public class GitCommit
{
    public string Hash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Author { get; set; } = string.Empty;
}

public class GitStatus
{
    public bool HasChanges { get; set; }
    public List<string> ModifiedFiles { get; set; } = new();
    public List<string> AddedFiles { get; set; } = new();
    public List<string> DeletedFiles { get; set; } = new();
}
