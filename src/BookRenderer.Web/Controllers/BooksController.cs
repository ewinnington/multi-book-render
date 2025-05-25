using Microsoft.AspNetCore.Mvc;

namespace BookRenderer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public BooksController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("{bookId}/assets/{*assetPath}")]
    public IActionResult GetBookAsset(string bookId, string assetPath)
    {
        // Validate inputs to prevent path traversal attacks
        if (string.IsNullOrWhiteSpace(bookId) || string.IsNullOrWhiteSpace(assetPath))
            return BadRequest();

        // Remove any path traversal attempts
        bookId = Path.GetFileName(bookId);
        assetPath = assetPath.Replace("..", "").Replace("\\", "/");

        // Construct the full path to the asset - need to find the Data folder from the web project
        var currentDir = _environment.ContentRootPath;
        var dataPath = Path.Combine(currentDir, "Data", "Books");
        
        // If Data folder doesn't exist at ContentRootPath, try going up to find it
        if (!Directory.Exists(dataPath))
        {
            var parentDir = Directory.GetParent(currentDir)?.FullName;
            if (parentDir != null)
            {
                dataPath = Path.Combine(parentDir, "Data", "Books");
            }
        }

        var assetFullPath = Path.Combine(dataPath, bookId, "assets", assetPath);

        // Ensure the file exists and is within the expected directory structure
        if (!System.IO.File.Exists(assetFullPath))
            return NotFound($"Asset not found: {assetPath}");

        // Verify the file is actually within the book's assets directory (security check)
        var bookAssetsPath = Path.Combine(dataPath, bookId, "assets");
        var normalizedAssetPath = Path.GetFullPath(assetFullPath);
        var normalizedBookAssetsPath = Path.GetFullPath(bookAssetsPath);
        
        if (!normalizedAssetPath.StartsWith(normalizedBookAssetsPath))
            return BadRequest("Invalid asset path");

        // Determine content type based on file extension
        var extension = Path.GetExtension(assetPath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        // Return the file with appropriate content type
        var fileBytes = System.IO.File.ReadAllBytes(assetFullPath);
        return File(fileBytes, contentType);
    }
}
