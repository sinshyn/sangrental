using System.Globalization;
using Microsoft.AspNetCore.DataProtection;

namespace SangRental.Admin.Services;

public sealed class FleetUiService(IDataProtectionProvider protection)
{
    private const string CookieName = "SangRental.FleetNotice";
    public void Saved(HttpContext context) => context.Response.Cookies.Append(CookieName,
        protection.CreateProtector("FleetNotice.v1").Protect("saved"), new CookieOptions
        { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/fleet", MaxAge = TimeSpan.FromMinutes(1) });

    public bool Consume(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-cache, no-store";
        context.Response.Headers.Pragma = "no-cache";
        if (context.Request.Cookies[CookieName] is not { } value) return false;
        context.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/fleet", Secure = true });
        try { return protection.CreateProtector("FleetNotice.v1").Unprotect(value) == "saved"; }
        catch (System.Security.Cryptography.CryptographicException) { return false; }
    }

    private static readonly TimeZoneInfo Vietnam = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
    public static string LocalTime(DateTime value) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), Vietnam)
        .ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static bool TryUtc(string? value, out DateTime utc)
    {
        utc = default;
        if (!DateTime.TryParseExact(value, ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)) return false;
        utc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Vietnam);
        return true;
    }
}
