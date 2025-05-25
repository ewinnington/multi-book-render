using BookRenderer.Core.Models;
using BookRenderer.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookRenderer.Web.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Username and password are required.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var user = await _userService.AuthenticateAsync(username, password);
        if (user == null)
        {
            ViewBag.Error = "Invalid username or password.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(120)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return RedirectToAction("Login");

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return RedirectToAction("Login");

        return View(user);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(User user)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null || user.Id != userId)
            return RedirectToAction("Login");

        try
        {
            // Don't allow users to change their own role
            var existingUser = await _userService.GetUserByIdAsync(userId);
            if (existingUser != null)
            {
                user.Role = existingUser.Role;
                user.CreatedAt = existingUser.CreatedAt;
            }

            await _userService.UpdateUserAsync(user);
            ViewBag.Success = "Profile updated successfully.";
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Error updating profile: {ex.Message}";
        }

        return View("Profile", user);
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            ViewBag.Error = "All fields are required.";
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "New password and confirmation do not match.";
            return View();
        }

        if (newPassword.Length < 6)
        {
            ViewBag.Error = "Password must be at least 6 characters long.";
            return View();
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return RedirectToAction("Login");

        var success = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);
        if (!success)
        {
            ViewBag.Error = "Current password is incorrect.";
            return View();
        }

        ViewBag.Success = "Password changed successfully.";
        return View();
    }
}
