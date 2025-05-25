using BookRenderer.Core.Models;
using BookRenderer.Core.Services;
using System.Text.Json;
using BCrypt.Net;

namespace BookRenderer.Services;

public class UserService : IUserService
{
    private readonly string _dataPath;
    private readonly string _usersFilePath;

    public UserService(string dataPath)
    {
        _dataPath = dataPath;
        _usersFilePath = Path.Combine(_dataPath, "Config", "users.json");
        
        // Ensure directories exist
        Directory.CreateDirectory(Path.GetDirectoryName(_usersFilePath)!);
        
        // Create default admin user if no users exist
        _ = Task.Run(async () => await EnsureDefaultAdminUserAsync());
    }    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        Console.WriteLine($"[DEBUG] Attempting to authenticate user: {username}");
        
        var user = await GetUserByUsernameAsync(username);
        if (user == null)
        {
            Console.WriteLine($"[DEBUG] User '{username}' not found");
            return null;
        }

        Console.WriteLine($"[DEBUG] User found: {user.Username}, Role: {user.Role}");
        Console.WriteLine($"[DEBUG] Stored password hash: {user.PasswordHash}");
        
        // Verify password
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        Console.WriteLine($"[DEBUG] Password verification result: {isPasswordValid}");
        
        if (!isPasswordValid)
            return null;

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await UpdateUserAsync(user);

        Console.WriteLine($"[DEBUG] Authentication successful for user: {username}");
        return user;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        var users = await LoadUsersAsync();
        return users.FirstOrDefault(u => u.Id.Equals(userId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var users = await LoadUsersAsync();
        return users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User> CreateUserAsync(User user, string password)
    {
        var users = await LoadUsersAsync();
        
        // Check if username already exists
        if (users.Any(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Username already exists");

        // Generate ID and hash password
        user.Id = Guid.NewGuid().ToString();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;

        users.Add(user);
        await SaveUsersAsync(users);

        return user;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        var users = await LoadUsersAsync();
        var existingUser = users.FirstOrDefault(u => u.Id == user.Id);
        
        if (existingUser == null)
            throw new InvalidOperationException("User not found");

        // Update properties (preserve password hash if not changing)
        existingUser.Username = user.Username;
        existingUser.Email = user.Email;
        existingUser.Role = user.Role;
        existingUser.AssignedBooks = user.AssignedBooks;
        existingUser.Preferences = user.Preferences;
        existingUser.LastLoginAt = user.LastLoginAt;
        
        // Only update password hash if it's provided
        if (!string.IsNullOrEmpty(user.PasswordHash))
            existingUser.PasswordHash = user.PasswordHash;

        await SaveUsersAsync(users);
        return existingUser;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var users = await LoadUsersAsync();
        var user = users.FirstOrDefault(u => u.Id == userId);
        
        if (user == null)
            return false;

        users.Remove(user);
        await SaveUsersAsync(users);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
            return false;

        // Verify old password
        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            return false;

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await UpdateUserAsync(user);
        
        return true;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await LoadUsersAsync();
    }    private async Task<List<User>> LoadUsersAsync()
    {
        if (!File.Exists(_usersFilePath))
            return new List<User>();

        var json = await File.ReadAllTextAsync(_usersFilePath);
        var options = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var users = JsonSerializer.Deserialize<List<User>>(json, options) ?? new List<User>();
        return users;
    }

    private async Task SaveUsersAsync(List<User> users)
    {
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(users, options);
        await File.WriteAllTextAsync(_usersFilePath, json);
    }

    private async Task EnsureDefaultAdminUserAsync()
    {
        var users = await LoadUsersAsync();
        if (users.Any())
            return; // Users already exist

        // Create default admin user
        var defaultAdmin = new User
        {
            Username = "admin",
            Email = "admin@example.com",
            Role = UserRole.Admin,
            AssignedBooks = new List<string>(), // Admin has access to all books
            Preferences = new UserPreferences()
        };

        await CreateUserAsync(defaultAdmin, "admin123"); // Default password - should be changed
        
        Console.WriteLine("Created default admin user - Username: admin, Password: admin123");
        Console.WriteLine("Please change the default password after first login!");
    }
}
