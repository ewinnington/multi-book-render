using BookRenderer.Core.Models;

namespace BookRenderer.Web.Models;

public class AdminDashboardViewModel
{
    public List<Book> Books { get; set; } = new();
    public List<User> Users { get; set; } = new();
}

public class ManageChaptersViewModel
{
    public Book Book { get; set; } = new();
    public List<Chapter> Chapters { get; set; } = new();
}
