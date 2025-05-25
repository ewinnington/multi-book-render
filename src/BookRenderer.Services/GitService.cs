using BookRenderer.Core.Services;
using LibGit2Sharp;

namespace BookRenderer.Services;

public class GitService : IGitService
{
    public Task<bool> InitializeRepositoryAsync(string bookPath)
    {
        try
        {
            if (!Directory.Exists(bookPath))
                Directory.CreateDirectory(bookPath);

            if (!Repository.IsValid(bookPath))
            {
                Repository.Init(bookPath);
                
                // Create initial commit
                using var repo = new Repository(bookPath);
                
                // Create .gitignore file
                var gitignorePath = Path.Combine(bookPath, ".gitignore");
                File.WriteAllText(gitignorePath, "*.tmp\n*.log\n");
                
                // Stage and commit
                Commands.Stage(repo, "*");
                var signature = new Signature("System", "system@bookrenderer.local", DateTimeOffset.Now);
                repo.Commit("Initial commit", signature, signature);
            }
            
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> CommitChangesAsync(string bookPath, string message)
    {
        try
        {
            using var repo = new Repository(bookPath);
            
            // Stage all changes
            Commands.Stage(repo, "*");
            
            // Check if there are any changes to commit
            var status = repo.RetrieveStatus();
            if (!status.IsDirty)
                return Task.FromResult(true);
            
            // Commit changes
            var signature = new Signature("System", "system@bookrenderer.local", DateTimeOffset.Now);
            repo.Commit(message, signature, signature);
            
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<IEnumerable<GitCommit>> GetCommitHistoryAsync(string bookPath)
    {
        try
        {
            using var repo = new Repository(bookPath);
            var commits = repo.Commits.Take(50).Select(commit => new GitCommit
            {
                Hash = commit.Sha[..8], // Short hash
                Message = commit.MessageShort,
                Date = commit.Author.When.DateTime,
                Author = commit.Author.Name
            }).ToList();
            
            return Task.FromResult(commits.AsEnumerable());
        }
        catch
        {
            return Task.FromResult(Enumerable.Empty<GitCommit>());
        }
    }

    public Task<GitStatus> GetStatusAsync(string bookPath)
    {
        try
        {
            using var repo = new Repository(bookPath);
            var status = repo.RetrieveStatus();
            
            var gitStatus = new GitStatus
            {
                HasChanges = status.IsDirty,
                ModifiedFiles = status.Modified.Select(f => f.FilePath).ToList(),
                AddedFiles = status.Added.Select(f => f.FilePath).ToList(),
                DeletedFiles = status.Missing.Select(f => f.FilePath).ToList()
            };
            
            return Task.FromResult(gitStatus);
        }
        catch
        {
            return Task.FromResult(new GitStatus());
        }
    }
}
