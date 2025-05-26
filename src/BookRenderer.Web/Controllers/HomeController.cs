using BookRenderer.Core.Services;
using BookRenderer.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookRenderer.Web.Controllers;

public class HomeController : BaseController
{
    private readonly IBookService _bookService;    public HomeController(IBookService bookService, IUserService userService) : base(userService)
    {
        _bookService = bookService;
    }

    public async Task<IActionResult> Index()
    {
        var allBooks = await _bookService.GetAllBooksAsync();
        var books = allBooks.ToList();
        
        // Filter books based on user access
        var filteredBooks = FilterBooksByAccess(books);
        
        return View(filteredBooks);
    }

    private List<Book> FilterBooksByAccess(List<Book> books)
    {
        var currentUser = GetCurrentUser();
        
        return books.Where(book => 
        {
            // Public books are visible to everyone
            if (book.IsPublic)
                return true;
            
            // Private books are only visible to logged-in users
            if (!User.Identity?.IsAuthenticated == true)
                return false;
            
            // If user is admin, they can see all books
            if (User.IsInRole("Admin"))
                return true;
            
            // Check if user is in the allowed users list for this private book
            if (currentUser != null && book.AllowedUsers != null)
            {
                return book.AllowedUsers.Contains(currentUser.Username, StringComparer.OrdinalIgnoreCase);
            }
            
            return false;
        }).ToList();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
