using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace WebBanHangOnline.Controllers;

public class LanguageController : Controller
{
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "vi",
        "en"
    };

    [HttpGet]
    public IActionResult SetLanguage(string culture, string? returnUrl = null)
    {
        if (!SupportedCultures.Contains(culture))
        {
            culture = "vi";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = Url.Action("Index", "Home") ?? "/";
        }

        return LocalRedirect(returnUrl);
    }
}
