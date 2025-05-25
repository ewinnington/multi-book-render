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
    }    public async Task<IActionResult> Read(string id, int? chapter = null)
    {
        if (string.IsNullOrEmpty(id))
            return NotFound();

        var book = await _bookService.GetBookByIdAsync(id);
        if (book == null)
            return NotFound();

        Console.WriteLine($"[DEBUG] BookController.Read: Book '{id}' found with {book.Chapters?.Count ?? 0} chapters");

        // Get all chapters for the book
        var allChapters = (await _chapterService.GetChaptersAsync(id)).OrderBy(c => c.Order).ToList();
        
        Console.WriteLine($"[DEBUG] BookController.Read: ChapterService returned {allChapters.Count} chapters");
        
        if (!allChapters.Any())
        {
            Console.WriteLine($"[DEBUG] BookController.Read: No chapters found, showing NoChapters view");
            ViewBag.Message = "This book doesn't have any chapters yet.";
            return View("NoChapters", book);
        }

        // Get the specific chapter or default to first chapter
        var chapterOrder = chapter ?? 1;
        var currentChapter = allChapters.FirstOrDefault(c => c.Order == chapterOrder) ?? allChapters.First();

        ViewBag.Book = book;
        ViewBag.CurrentChapter = currentChapter;
        ViewBag.AllChapters = allChapters;

        return View("Read", currentChapter);
    }
}
