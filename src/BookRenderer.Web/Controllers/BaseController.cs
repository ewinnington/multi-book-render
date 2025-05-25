using BookRenderer.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BookRenderer.Web.Controllers;

public class BaseController : Controller
{
    private readonly IUserService? _userService;

    public BaseController(IUserService? userService = null)
    {
        _userService = userService;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Set theme for authenticated users
        if (User?.Identity?.IsAuthenticated == true && _userService != null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    ViewBag.UserTheme = user?.Preferences?.Theme ?? "light";
                }
            }
            catch
            {
                // If there's an error fetching user theme, default to light
                ViewBag.UserTheme = "light";
            }
        }
        else
        {
            ViewBag.UserTheme = "light";
        }

        await next();
    }
}
