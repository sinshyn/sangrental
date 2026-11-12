using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SangRental.Application.CatalogFleet;
using SangRental.Infrastructure.Persistence;

namespace SangRental.Infrastructure.Security;

public static class AdminIdentityServices
{
    public const string PermissionClaim = "permission";
    public const string ManageFleet = "fleet.manage";
    public const string ApproveFleet = "fleet.approve";
    public const string PublishCatalog = "catalog.publish";

    public static IServiceCollection AddAdminIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var protection = services.AddDataProtection().SetApplicationName("SangRental.Admin.Identity.v1");
        if (configuration["DataProtection:KeyDirectory"] is { Length: > 0 } keyDirectory)
        {
            protection.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
            if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        }

        services.AddIdentity<AdminUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequiredUniqueChars = 4;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = false; // Internal usernames; email delivery is not configured yet.
        }).AddEntityFrameworkStores<SangRentalDbContext>().AddDefaultTokenProviders();
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-SangRental.Admin";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/account/access-denied";
            options.EventsType = typeof(AdminCookieEvents);
        });
        services.AddScoped<AdminCookieEvents>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICatalogFleetActorSource, HttpCatalogFleetActorSource>();
        services.AddScoped<CatalogFleetCommandService>();
        services.AddScoped<CatalogFleetSetupService>();
        services.AddScoped<AdminSignInService>();
        services.AddScoped<AdminProvisioningService>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminAccess", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
            foreach (var permission in new[] { ManageFleet, ApproveFleet, PublishCatalog })
                options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
                    context.User.IsInRole("Admin") || context.User.IsInRole("Manager")
                    && context.User.HasClaim(PermissionClaim, permission)));
        });
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-SangRental.Antiforgery";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.HttpOnly = true;
        });
        return services;
    }
}

public sealed class AdminCookieEvents(ISecurityStampValidator stampValidator, UserManager<AdminUser> users)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        await stampValidator.ValidateAsync(context);
        if (context.Principal?.Identity?.IsAuthenticated != true) return;
        var user = await users.GetUserAsync(context.Principal);
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user)) context.RejectPrincipal();
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth"))
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth"))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
}

public sealed class HttpCatalogFleetActorSource(IHttpContextAccessor accessor) : ICatalogFleetActorSource
{
    public CatalogFleetActor GetRequiredActor()
    {
        var principal = accessor.HttpContext?.User;
        var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal?.Identity?.IsAuthenticated != true || !Guid.TryParse(id, out _))
            throw new UnauthorizedAccessException("An authenticated server-side actor is required.");
        bool Has(string permission) => principal.IsInRole("Manager")
            && principal.HasClaim(AdminIdentityServices.PermissionClaim, permission);
        return new CatalogFleetActor(id, principal.IsInRole("Admin"),
            Has(AdminIdentityServices.ManageFleet), Has(AdminIdentityServices.ApproveFleet),
            Has(AdminIdentityServices.PublishCatalog));
    }
}
