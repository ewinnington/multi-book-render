using BookRenderer.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookRenderer.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChapterController : ControllerBase
{
    private readonly IChapterService _chapterService;
    private readonly IMarkdownService _markdownService;

    public ChapterController(IChapterService chapterService, IMarkdownService markdownService)
    {
        _chapterService = chapterService;
        _markdownService = markdownService;
    }

    [HttpGet("{bookId}/{chapterId}")]
    public async Task<IActionResult> GetChapter(string bookId, string chapterId)
    {
        var chapter = await _chapterService.GetChapterAsync(bookId, chapterId);
        if (chapter == null)
            return NotFound();

        return Ok(chapter);
    }

    [HttpGet("{bookId}/{chapterId}/render")]
    public async Task<IActionResult> GetRenderedChapter(string bookId, string chapterId)
    {
        var chapter = await _chapterService.GetChapterAsync(bookId, chapterId);
        if (chapter == null)
            return NotFound();

        var html = await _markdownService.RenderMarkdownAsync(chapter.Content, bookId);
        return Ok(new { html });
    }

    [HttpGet("{bookId}")]
    public async Task<IActionResult> GetChapters(string bookId)
    {
        var chapters = await _chapterService.GetChaptersAsync(bookId);
        return Ok(chapters);
    }
}
