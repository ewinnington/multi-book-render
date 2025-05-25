using BookRenderer.Core.Models;
using BookRenderer.Core.Services;
using BookRenderer.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookRenderer.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly IBookService _bookService;
    private readonly IUserService _userService;
    private readonly IChapterService _chapterService;

    public AdminController(IBookService bookService, IUserService userService, IChapterService chapterService) : base(userService)
    {
        _bookService = bookService;
        _userService = userService;
        _chapterService = chapterService;
    }

    public async Task<IActionResult> Index()
    {
        var books = await _bookService.GetAllBooksAsync();
        var users = await _userService.GetAllUsersAsync();
        
        var viewModel = new AdminDashboardViewModel
        {
            Books = books.ToList(),
            Users = users.ToList()
        };

        return View(viewModel);
    }

    #region Book Management

    [HttpGet]
    public IActionResult CreateBook()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateBook(Book book)
    {
        if (!ModelState.IsValid)
            return View(book);

        try
        {
            var createdBook = await _bookService.CreateBookAsync(book);
            TempData["Success"] = $"Book '{createdBook.Title}' created successfully.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Error creating book: {ex.Message}";
            return View(book);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditBook(string id)
    {
        var book = await _bookService.GetBookByIdAsync(id);
        if (book == null)
            return NotFound();

        return View(book);
    }

    [HttpPost]
    public async Task<IActionResult> EditBook(Book book)
    {
        if (!ModelState.IsValid)
            return View(book);

        try
        {
            await _bookService.UpdateBookAsync(book);
            TempData["Success"] = $"Book '{book.Title}' updated successfully.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Error updating book: {ex.Message}";
            return View(book);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBook(string id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book != null)
            {
                await _bookService.DeleteBookAsync(id);
                TempData["Success"] = $"Book '{book.Title}' deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting book: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    #endregion

    #region Chapter Management

    [HttpGet]
    public async Task<IActionResult> ManageChapters(string bookId)
    {
        var book = await _bookService.GetBookByIdAsync(bookId);
        if (book == null)
            return NotFound();

        var chapters = await _chapterService.GetChaptersAsync(bookId);
        
        var viewModel = new ManageChaptersViewModel
        {
            Book = book,
            Chapters = chapters.OrderBy(c => c.Order).ToList()
        };

        return View(viewModel);
    }    [HttpGet]
    public async Task<IActionResult> CreateChapter(string bookId)
    {
        var book = await _bookService.GetBookByIdAsync(bookId);
        if (book == null)
            return NotFound();

        var chapters = await _chapterService.GetChaptersAsync(bookId);
        var nextOrder = chapters.Any() ? chapters.Max(c => c.Order) + 1 : 1;

        var chapter = new Chapter
        {
            Id = "", // Will be generated during creation
            BookId = bookId,
            Order = nextOrder,
            Title = $"Chapter {nextOrder}",
            Content = $"# Chapter {nextOrder}\n\nStart writing your content here...",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ViewBag.Book = book;
        ViewBag.NextChapterOrder = nextOrder;
        return View(chapter);
    }

    [HttpPost]
    public async Task<IActionResult> CreateChapter(Chapter chapter)
    {
        // Add debugging information
        Console.WriteLine($"[DEBUG] CreateChapter POST - BookId: {chapter.BookId}");
        Console.WriteLine($"[DEBUG] CreateChapter POST - Title: {chapter.Title}");
        Console.WriteLine($"[DEBUG] CreateChapter POST - Order: {chapter.Order}");
        Console.WriteLine($"[DEBUG] CreateChapter POST - Content length: {chapter.Content?.Length ?? 0}");
        Console.WriteLine($"[DEBUG] CreateChapter POST - ModelState valid: {ModelState.IsValid}");
        
        if (!ModelState.IsValid)
        {
            Console.WriteLine("[DEBUG] CreateChapter POST - ModelState errors:");
            foreach (var error in ModelState)
            {
                Console.WriteLine($"[DEBUG] Field {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
            }
            
            var book = await _bookService.GetBookByIdAsync(chapter.BookId);
            ViewBag.Book = book;
            return View(chapter);
        }

        try
        {
            Console.WriteLine("[DEBUG] CreateChapter POST - Calling ChapterService.CreateChapterAsync");
            await _chapterService.CreateChapterAsync(chapter);
            TempData["Success"] = $"Chapter '{chapter.Title}' created successfully.";
            Console.WriteLine("[DEBUG] CreateChapter POST - Chapter created successfully");
            return RedirectToAction("ManageChapters", new { bookId = chapter.BookId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] CreateChapter POST - Exception: {ex.Message}");
            Console.WriteLine($"[DEBUG] CreateChapter POST - StackTrace: {ex.StackTrace}");
            ViewBag.Error = $"Error creating chapter: {ex.Message}";
            var book = await _bookService.GetBookByIdAsync(chapter.BookId);
            ViewBag.Book = book;
            return View(chapter);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditChapter(string bookId, string chapterId)
    {
        var chapter = await _chapterService.GetChapterAsync(bookId, chapterId);
        if (chapter == null)
            return NotFound();

        var book = await _bookService.GetBookByIdAsync(bookId);
        ViewBag.Book = book;
        
        return View(chapter);
    }

    [HttpPost]
    public async Task<IActionResult> EditChapter(Chapter chapter)
    {
        if (!ModelState.IsValid)
        {
            var book = await _bookService.GetBookByIdAsync(chapter.BookId);
            ViewBag.Book = book;
            return View(chapter);
        }

        try
        {
            await _chapterService.UpdateChapterAsync(chapter);
            TempData["Success"] = $"Chapter '{chapter.Title}' updated successfully.";
            return RedirectToAction("ManageChapters", new { bookId = chapter.BookId });
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Error updating chapter: {ex.Message}";
            var book = await _bookService.GetBookByIdAsync(chapter.BookId);
            ViewBag.Book = book;
            return View(chapter);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteChapter(string bookId, string chapterId)
    {
        try
        {
            await _chapterService.DeleteChapterAsync(bookId, chapterId);
            TempData["Success"] = "Chapter deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting chapter: {ex.Message}";
        }

        return RedirectToAction("ManageChapters", new { bookId });
    }

    #endregion

    #region User Management

    [HttpGet]
    public async Task<IActionResult> ManageUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return View(users.ToList());
    }

    [HttpGet]
    public IActionResult CreateUser()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            ViewBag.Error = "Password must be at least 6 characters long.";
            return View(user);
        }

        try
        {
            await _userService.CreateUserAsync(user, password);
            TempData["Success"] = $"User '{user.Username}' created successfully.";
            return RedirectToAction("ManageUsers");
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Error creating user: {ex.Message}";
            return View(user);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (id == currentUserId)
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction("ManageUsers");
        }

        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user != null)
            {
                await _userService.DeleteUserAsync(id);
                TempData["Success"] = $"User '{user.Username}' deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error deleting user: {ex.Message}";
        }        return RedirectToAction("ManageUsers");
    }

    #endregion
}
