using BookRenderer.Core.Services;
using BookRenderer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register services
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<IGitService, GitService>();
builder.Services.AddScoped<IMarkdownService, MarkdownService>();

// Configure data path for BookService
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "..", "Data");
builder.Services.AddScoped<IBookService>(provider => 
    new BookService(
        provider.GetRequiredService<IFileSystemService>(),
        provider.GetRequiredService<IGitService>(),
        dataPath));

builder.Services.AddScoped<IChapterService, ChapterService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
