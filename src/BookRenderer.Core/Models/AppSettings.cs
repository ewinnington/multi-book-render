namespace BookRenderer.Core.Models;

public class AppSettings
{
    public string SiteName { get; set; } = "Multi-Book Renderer";
    public string DataPath { get; set; } = "Data";
    public bool AllowRegistration { get; set; } = false;
    public bool RequireAuthentication { get; set; } = false;
    public CodeExecutionSettings CodeExecution { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
}

public class CodeExecutionSettings
{
    public bool Enabled { get; set; } = false;
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int MaxOutputLength { get; set; } = 10000;
    public List<string> AllowedLanguages { get; set; } = new() { "javascript", "python", "csharp" };
}

public class SecuritySettings
{
    public int SessionTimeoutMinutes { get; set; } = 120;
    public int MaxFileUploadSizeMB { get; set; } = 10;
    public List<string> AllowedImageExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
}
