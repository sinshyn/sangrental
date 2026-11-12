using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Components.Authorization;
using SangRental.Admin.Components;
using SangRental.Admin.Services;
using SangRental.Application;
using SangRental.Infrastructure;
using SangRental.Infrastructure.Media;
using SangRental.Infrastructure.Security;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--test-database").ToArray());
if (args.Contains("--test-database", StringComparer.Ordinal)) builder.Configuration.UseLocalTestDatabase();
// Keep local/server logging independent of Windows Event Log permissions.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<SangRental.Admin.Services.NotificationService>();
builder.Services.AddScoped<FleetUiService>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAdminIdentity(builder.Configuration);
builder.Services.AddScoped<AuthenticationStateProvider, AdminRevalidatingAuthenticationStateProvider>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/auth")
    && !context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/media")
    && !context.Request.Path.StartsWithSegments("/health"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();
app.MapPost("/auth/login", async (HttpContext context, IAntiforgery antiforgery, AdminSignInService signIn) =>
{
    if (!context.Request.IsHttps) return Results.BadRequest();
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    var form = await context.Request.ReadFormAsync(context.RequestAborted);
    context.Response.Headers.CacheControl = "no-store";
    try
    {
        var succeeded = await signIn.SignInAsync(form["username"].ToString(), form["password"].ToString(), context.RequestAborted);
        return Results.LocalRedirect(succeeded ? "/" : "/account/login?error=invalid");
    }
    catch (Exception) { return Results.LocalRedirect("/account/login?error=unavailable"); }
}).AllowAnonymous().RequireRateLimiting("login");
app.MapPost("/auth/logout", async (HttpContext context, IAntiforgery antiforgery, AdminSignInService signIn) =>
{
    if (!context.Request.IsHttps) return Results.BadRequest();
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    await signIn.SignOutAsync(context.RequestAborted);
    return Results.LocalRedirect("/account/login");
}).RequireAuthorization();
app.MapStaticAssets().AllowAnonymous();
app.MapCatalogMediaEndpoints(enableUiRedirects: true);
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();
app.MapGet("/auth/session", () => Results.NoContent()).RequireAuthorization("AdminAccess");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
public partial class Program;
