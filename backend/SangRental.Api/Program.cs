using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SangRental.Application;
using SangRental.Infrastructure;
using SangRental.Infrastructure.Media;
using SangRental.Infrastructure.Security;
using SangRental.Api;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
var builder = WebApplication.CreateBuilder(args.Where(a => a is not "--provision-admin" and not "--test-database").ToArray());
if (args.Contains("--test-database", StringComparer.Ordinal)) builder.Configuration.UseLocalTestDatabase();
// Console logging must not require Windows Event Log write permissions.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAdminIdentity(builder.Configuration);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddOpenApi(options => options.AddOperationTransformer((operation, context, cancellationToken) =>
{
    var path = context.Description.RelativePath ?? "";
    if (path == "api/admin/fleet/tasks/{id}" || path == "api/admin/fleet/maintenance/{id}/complete")
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "If-Match", In = ParameterLocation.Header, Required = true,
            Description = "Quoted Base64 rowVersion from the latest GET response, for example \"AAAAAAAAB9E=\".",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }
    return Task.CompletedTask;
}));
builder.Services.AddProblemDetails();
var app = builder.Build();
if (args.Contains("--provision-admin", StringComparer.Ordinal))
{
    try { await AdminBootstrapCommand.RunAsync(app.Services, args.Contains("--test-database", StringComparer.Ordinal)); }
    catch (Exception) { Console.Error.WriteLine("Không thể tạo Admin. Kiểm tra migration Identity, tài khoản đã có và yêu cầu mật khẩu."); Environment.ExitCode = 1; }
    return;
}
app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SangRental Admin API v1");
        options.UseRequestInterceptor("async (request) => { if (!['GET', 'HEAD', 'OPTIONS'].includes(request.method.toUpperCase())) { const response = await fetch('/auth/csrf', { credentials: 'same-origin', cache: 'no-store' }); if (!response.ok) throw new Error('HTTPS and a valid antiforgery token are required.'); const token = await response.json(); request.headers['X-CSRF-TOKEN'] = token.requestToken; } request.credentials = 'same-origin'; return request; }");
    });
}
app.MapAdminEndpoints();
app.MapCatalogMediaEndpoints();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.Run();
public partial class Program;
