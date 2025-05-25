using BookRenderer.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookRenderer.Web.Controllers;

public class BookController : Controller
{
    private readonly IBookService _bookService;
    private readonly IChapterService _chapterService;

    public BookController(IBookService bookService, IChapterService chapterService)
    {
        _bookService = bookService;
        _chapterService = chapterService;
    }    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var book = await _bookService.GetBookByIdAsync(id);
        if (book == null)
            return NotFound();

        return View(book);
    }

    public async Task<IActionResult> Read(string id, int? chapter = null)
    {
        // Add logging to debug the issue
        Console.WriteLine($"[DEBUG] BookController.Read called with id='{id}', chapter={chapter}");
        
        if (string.IsNullOrEmpty(id))
        {
            Console.WriteLine("[DEBUG] id parameter is null or empty, returning NotFound");
            return NotFound();
        }

        var book = await _bookService.GetBookByIdAsync(id);
        if (book == null)
        {
            Console.WriteLine($"[DEBUG] Book with id '{id}' not found");
            return NotFound();
        }

        // Get the specific chapter or default to first chapter
        var chapterOrder = chapter ?? 1;
        var currentChapter = await _chapterService.GetChapterByOrderAsync(id, chapterOrder);
        
        if (currentChapter == null && book.Chapters.Any())
        {
            // Fall back to first available chapter
            currentChapter = book.Chapters.OrderBy(c => c.Order).First();
        }

        if (currentChapter == null)
        {
            // No chapters available
            ViewBag.Message = "This book doesn't have any chapters yet.";
            return View("NoChapters", book);
        }

        ViewBag.Book = book;
        ViewBag.CurrentChapter = currentChapter;
        ViewBag.AllChapters = book.Chapters.OrderBy(c => c.Order).ToList();

        return View("Read", currentChapter);
    }
}
