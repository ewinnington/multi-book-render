using BookRenderer.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookRenderer.Web.Controllers;

public class HomeController : BaseController
{
    private readonly IBookService _bookService;

    public HomeController(IBookService bookService, IUserService userService) : base(userService)
    {
        _bookService = bookService;
    }

    public async Task<IActionResult> Index()
    {
        // For now, show all books (will add user filtering later)
        var books = await _bookService.GetAllBooksAsync();
        return View(books);
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
