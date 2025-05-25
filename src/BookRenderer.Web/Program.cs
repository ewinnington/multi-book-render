using BookRenderer.Core.Services;
using BookRenderer.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(120); // 2 hours
        options.SlidingExpiration = true;
    });

// Register services
builder.Services.AddScoped<IFileSystemService, FileSystemService>();
builder.Services.AddScoped<IGitService, GitService>();
builder.Services.AddScoped<IMarkdownService, MarkdownService>();

// Configure data path for services
var dataPath = Path.Combine(builder.Environment.ContentRootPath, "..", "Data");
builder.Services.AddScoped<IBookService>(provider => 
    new BookService(
        provider.GetRequiredService<IFileSystemService>(),
        provider.GetRequiredService<IGitService>(),
        dataPath));

builder.Services.AddScoped<IChapterService>(provider =>
    new ChapterService(
        provider.GetRequiredService<IFileSystemService>(),
        provider.GetRequiredService<IGitService>(),
        provider.GetRequiredService<IBookService>()));

builder.Services.AddScoped<IUserService>(provider => new UserService(dataPath));

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
