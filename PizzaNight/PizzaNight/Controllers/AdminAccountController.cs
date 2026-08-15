using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PizzaNight.Models;
using PizzaNight.Services;

namespace PizzaNight.Controllers;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class AdminAccountController(AdminCredentialValidator credentialValidator) : Controller
{
    [AllowAnonymous]
    [HttpGet("/admin/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "AdminOrders");
        }

        return View(new AdminLoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("/admin/login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("admin-login")]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid || !credentialValidator.IsValid(model.Username, model.Password))
        {
            ModelState.AddModelError(string.Empty, "The username or password is incorrect.");
            model.Password = string.Empty;
            return View(model);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, model.Username.Trim()),
            new Claim(ClaimTypes.Role, "Administrator")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 24 : 8)
            });

        return Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl)
            : RedirectToAction("Index", "AdminOrders");
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("/admin/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet("/admin/access-denied")]
    public IActionResult AccessDenied() => View();
}
