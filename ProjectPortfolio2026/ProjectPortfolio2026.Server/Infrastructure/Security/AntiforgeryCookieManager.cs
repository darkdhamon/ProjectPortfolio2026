using Microsoft.AspNetCore.Antiforgery;

namespace ProjectPortfolio2026.Server.Infrastructure.Security;

public static class AntiforgeryCookieManager
{
    public const string HeaderName = "X-XSRF-TOKEN";
    public const string RequestTokenCookieName = "XSRF-TOKEN";

    public static void IssueRequestTokenCookie(HttpContext context, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        if (string.IsNullOrWhiteSpace(tokens.RequestToken))
        {
            return;
        }

        context.Response.Cookies.Append(
            RequestTokenCookieName,
            tokens.RequestToken,
            new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Path = "/"
            });
    }

    public static void DeleteRequestTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(
            RequestTokenCookieName,
            new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });
    }
}
