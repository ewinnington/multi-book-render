using BookRenderer.Core.Services;
using BookRenderer.Core.Models;
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
        // Set theme and preferences for authenticated users
        if (User?.Identity?.IsAuthenticated == true && _userService != null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    ViewBag.UserTheme = user?.Preferences?.Theme ?? "light";
                    ViewBag.UserEnableCodeExecution = user?.Preferences?.EnableCodeExecution ?? false;
                }
            }
            catch
            {
                // If there's an error fetching user theme, default to light
                ViewBag.UserTheme = "light";
                ViewBag.UserEnableCodeExecution = false;
            }
        }
        else
        {
            ViewBag.UserTheme = "light";
            ViewBag.UserEnableCodeExecution = false;
        }        await next();
    }

    protected User? GetCurrentUser()
    {
        if (User?.Identity?.IsAuthenticated != true || _userService == null)
            return null;

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Note: This is async but we can't make this method async without changing all controllers
                // For now, we'll use a synchronous approach or cache the user
                return _userService.GetUserByIdAsync(userId).GetAwaiter().GetResult();
            }
        }
        catch
        {
            // If there's an error, return null
        }

        return null;
    }
}
