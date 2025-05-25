using BookRenderer.Core.Services;

namespace BookRenderer.Services;

public class FileSystemService : IFileSystemService
{
    public async Task<string> ReadFileAsync(string filePath)
    {
        if (!await FileExistsAsync(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
            
        return await File.ReadAllTextAsync(filePath);
    }

    public async Task WriteFileAsync(string filePath, string content)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !await DirectoryExistsAsync(directory))
        {
            await CreateDirectoryAsync(directory);
        }
        
        await File.WriteAllTextAsync(filePath, content);
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<bool> DirectoryExistsAsync(string directoryPath)
    {
        return Task.FromResult(Directory.Exists(directoryPath));
    }

    public Task CreateDirectoryAsync(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public Task DeleteDirectoryAsync(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
            Directory.Delete(directoryPath, true);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetFilesAsync(string directoryPath, string pattern = "*")
    {
        if (!Directory.Exists(directoryPath))
            return Task.FromResult(Enumerable.Empty<string>());
            
        var files = Directory.GetFiles(directoryPath, pattern);
        return Task.FromResult(files.AsEnumerable());
    }

    public Task<IEnumerable<string>> GetDirectoriesAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return Task.FromResult(Enumerable.Empty<string>());
            
        var directories = Directory.GetDirectories(directoryPath);
        return Task.FromResult(directories.AsEnumerable());
    }
}
