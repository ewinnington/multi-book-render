namespace BookRenderer.Core.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Reader;
    public List<string> AssignedBooks { get; set; } = new();
    public UserPreferences Preferences { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; }
}

public enum UserRole
{
    Reader,
    Admin
}

public class UserPreferences
{
    public string Theme { get; set; } = "light";
    public int FontSize { get; set; } = 16;
    public bool ShowTableOfContents { get; set; } = true;
    public bool EnableCodeExecution { get; set; } = false;
}
