namespace BookRenderer.Core.Models;

public class Book
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string CoverColor { get; set; } = "#4f46e5"; // Default blue
    public string? CoverImagePath { get; set; }
    public bool IsPublic { get; set; } = true;
    public List<string> AllowedUsers { get; set; } = new();
    public List<Chapter> Chapters { get; set; } = new();
    public string GitRepositoryPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public BookSettings Settings { get; set; } = new();
}

public class Chapter
{
    public string Id { get; set; } = string.Empty;
    public string BookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class BookSettings
{
    public bool AllowCodeExecution { get; set; } = false;
    public List<string> AllowedCodeLanguages { get; set; } = new();
    public int CodeExecutionTimeoutSeconds { get; set; } = 30;
    public bool ShowLineNumbers { get; set; } = true;
}
