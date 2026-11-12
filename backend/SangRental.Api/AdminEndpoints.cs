using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SangRental.Application.CatalogFleet;
using SangRental.Domain.CatalogFleet;
using SangRental.Infrastructure.Persistence;
using SangRental.Infrastructure.Security;

namespace SangRental.Api;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            if (!context.Request.IsHttps) return Problem(400, "auth.https_required");
            context.Response.Headers.CacheControl = "no-cache, no-store";
            context.Response.Headers.Pragma = "no-cache";
            return Results.Ok(new { requestToken = antiforgery.GetAndStoreTokens(context).RequestToken });
        }).AllowAnonymous();
        app.MapPost("/auth/login", async (LoginRequest request, AdminSignInService signIn, CancellationToken ct) =>
            await signIn.SignInAsync(request.Username, request.Password, ct)
                ? Results.NoContent() : Problem(401, "auth.invalid_credentials"))
            .AllowAnonymous().RequireRateLimiting("login").AddEndpointFilter<SecureMutationFilter>();
        app.MapPost("/auth/logout", async (AdminSignInService signIn, CancellationToken ct) =>
        {
            await signIn.SignOutAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization().AddEndpointFilter<SecureMutationFilter>();
        app.MapGet("/auth/session", (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new
            {
                userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value),
                permissions = context.User.FindAll(AdminIdentityServices.PermissionClaim).Select(c => c.Value)
            });
        }).RequireAuthorization();

        var catalog = app.MapGroup("/api/admin/catalog").WithTags("Admin Catalog").RequireAuthorization("AdminAccess");
        catalog.MapGet("/categories", async (SangRentalDbContext db, int? page, CancellationToken ct) =>
            await db.Set<VehicleCategory>().AsNoTracking().OrderBy(x => x.Id).Skip(Skip(page)).Take(50)
                .Select(x => new CategorySummary(x.Id, x.Code, x.VehicleFamilyCode, x.PublicationStatus)).ToListAsync(ct));
        catalog.MapGet("/brands", async (SangRentalDbContext db, int? page, CancellationToken ct) =>
            await db.Set<VehicleBrand>().AsNoTracking().OrderBy(x => x.Id).Skip(Skip(page)).Take(50)
                .Select(x => new BrandSummary(x.Id, x.Code, x.DisplayName)).ToListAsync(ct));
        catalog.MapGet("/models", async (SangRentalDbContext db, int? page, CancellationToken ct) =>
            await db.Set<VehicleModel>().AsNoTracking().OrderBy(x => x.Id).Skip(Skip(page)).Take(50)
                .Select(x => new ModelSummary(x.Id, x.Code, x.CommercialName, x.PublicationStatus)).ToListAsync(ct));
        catalog.MapPost("/categories", async (CreateCategoryRequest request, CatalogFleetSetupService service, CancellationToken ct) =>
            Results.Created("/api/admin/catalog/categories", new { id = await service.CreateCategoryAsync(request, ct) }))
            .AddEndpointFilter<SecureMutationFilter>();
        catalog.MapPost("/brands", async (CreateBrandRequest request, CatalogFleetSetupService service, CancellationToken ct) =>
            Results.Created("/api/admin/catalog/brands", new { id = await service.CreateBrandAsync(request, ct) }))
            .AddEndpointFilter<SecureMutationFilter>();
        catalog.MapPost("/models", async (CreateModelRequest request, CatalogFleetSetupService service, CancellationToken ct) =>
            Results.Created("/api/admin/catalog/models", new { id = await service.CreateModelAsync(request, ct) }))
            .AddEndpointFilter<SecureMutationFilter>();

        var fleet = app.MapGroup("/api/admin/fleet").WithTags("Admin Fleet").RequireAuthorization(AdminIdentityServices.ManageFleet);
        fleet.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return await next(context);
        });
        fleet.MapGet("/vehicles/{id:long}", async (long id, SangRentalDbContext db, CancellationToken ct) =>
        {
            var vehicle = await db.Set<PhysicalVehicle>().AsNoTracking().Where(x => x.Id == id)
                .Select(x => new VehicleDetails(x.Id, x.VehicleModelId, x.AssetCode, x.VinOrChassisNumber,
                    x.RecordLifecycle, x.OperationalStatus, x.ReadyForRentAtUtc, x.RetiredAtUtc,
                    x.RetirementReason, Convert.ToBase64String(x.RowVersion), x.ColorName, x.ManufacturingYear)).SingleOrDefaultAsync(ct);
            return vehicle is null ? Results.NotFound() : Results.Ok(vehicle);
        });
        fleet.MapPut("/vehicles/{id:long}/profile", async (long id, UpdateVehicleProfileRequest request,
            HttpContext context, CatalogFleetCommandService service, CancellationToken ct) =>
        {
            await service.UpdateVehicleProfileAsync(id, request, Version(context), ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminAccess").AddEndpointFilter<SecureMutationFilter>();
        fleet.MapGet("/vehicles/{id:long}/registrations", async (long id, SangRentalDbContext db, int? page, CancellationToken ct) =>
            await db.Set<VehicleRegistration>().AsNoTracking().Where(x => x.PhysicalVehicleId == id)
                .OrderByDescending(x => x.ValidFromUtc).ThenByDescending(x => x.Id).Skip(Skip(page)).Take(50)
                .Select(x => new VehicleRegistrationSummary(x.Id, x.RegistrationJurisdictionCode, x.PlateDisplay,
                    x.ValidFromUtc, x.ValidToUtc, x.ClosureReason)).ToListAsync(ct));
        fleet.MapPost("/vehicles/{id:long}/registrations", async (long id, ChangeVehicleRegistrationRequest request,
            HttpContext context, CatalogFleetCommandService service, CancellationToken ct) =>
        {
            await service.ChangeVehicleRegistrationAsync(id, request, Version(context), ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminAccess").AddEndpointFilter<SecureMutationFilter>();
        fleet.MapPost("/vehicles/{id:long}/activate", async (long id, ChangeVehicleLifecycleRequest request,
            HttpContext context, CatalogFleetCommandService service, CancellationToken ct) =>
        {
            await service.ActivateVehicleAsync(id, request, Version(context), ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminAccess").AddEndpointFilter<SecureMutationFilter>();
        fleet.MapPost("/vehicles/{id:long}/retire", async (long id, ChangeVehicleLifecycleRequest request,
            HttpContext context, CatalogFleetCommandService service, CancellationToken ct) =>
        {
            await service.RetireVehicleAsync(id, request, Version(context), ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminAccess").AddEndpointFilter<SecureMutationFilter>();
        fleet.MapPost("/vehicles/{id:long}/restore", async (long id, ChangeVehicleLifecycleRequest request,
            HttpContext context, CatalogFleetCommandService service, CancellationToken ct) =>
        {
            await service.RestoreVehicleAsync(id, request, Version(context), ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminAccess").AddEndpointFilter<SecureMutationFilter>();
        fleet.MapGet("/vehicles", async (SangRentalDbContext db, int? page, CancellationToken ct) =>
            await db.Set<PhysicalVehicle>().AsNoTracking().OrderBy(x => x.Id).Skip(Skip(page)).Take(50)
                .Select(x => new VehicleSummary(x.Id, x.VehicleModelId, x.AssetCode, x.RecordLifecycle,
                    x.OperationalStatus, Convert.ToBase64String(x.RowVersion))).ToListAsync(ct));
        fleet.MapPost("/vehicles", async (CreateVehicleRequest request, CatalogFleetSetupService setup,
            CatalogFleetCommandService service, CancellationToken ct) =>
        {
            CatalogFleetSetupService.Validate(request);
            var vin = string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim();
            var id = request.ConfirmInitialInspection
                ? await service.CreateReadyVehicleAsync(new PhysicalVehicle { VehicleModelId = request.ModelId,
                    AssetCode = request.AssetCode.Trim(), VinOrChassisNumber = vin,
                    NormalizedVinOrChassisNumber = vin?.ToUpperInvariant() }, request.DuplicateVinReason, ct)
                : await setup.CreateDraftVehicleAsync(request, ct);
            return Results.Created("/api/admin/fleet/vehicles", new { id });
        }).RequireAuthorization("AdminAccess").AddEndpointFilter<SecureMutationFilter>();
        fleet.MapGet("/vehicles/{id:long}/maintenance", async (long id, SangRentalDbContext db, int? page, CancellationToken ct) =>
            await db.Set<MaintenanceEpisode>().AsNoTracking().Where(x => x.PhysicalVehicleId == id)
                .OrderBy(x => x.Id).Skip(Skip(page)).Take(50)
                .Select(x => new MaintenanceSummary(x.Id, x.PhysicalVehicleId, x.ScheduledStartUtc, x.ScheduledEndUtc,
                    x.StatusCode, x.ActualEndUtc, Convert.ToBase64String(x.RowVersion))).ToListAsync(ct));
        fleet.MapPost("/vehicles/{id:long}/maintenance", async (long id, ScheduleMaintenanceRequest request,
            CatalogFleetCommandService service, CancellationToken ct) =>
        {
            CatalogFleetSetupService.Validate(request);
            return Results.Created($"/api/admin/fleet/vehicles/{id}/maintenance", new
            { id = await service.ScheduleMaintenanceAsync(id, request.ScheduledStartUtc, request.ScheduledEndUtc, request.Reason, ct) });
        }).AddEndpointFilter<SecureMutationFilter>();
        fleet.MapGet("/maintenance/{id:long}/tasks", async (long id, SangRentalDbContext db, CancellationToken ct) =>
            await db.Set<MaintenanceChecklistItem>().AsNoTracking().Where(x => x.MaintenanceEpisodeId == id)
                .OrderBy(x => x.Id).Take(200).Select(x => new MaintenanceTaskSummary(x.Id, x.TitleSnapshotVi,
                    x.StatusCode, Convert.ToBase64String(x.RowVersion))).ToListAsync(ct));
        fleet.MapPost("/maintenance/{id:long}/tasks", async (long id, AddMaintenanceTaskRequest request,
            CatalogFleetCommandService service, CancellationToken ct) =>
        {
            CatalogFleetSetupService.Validate(request);
            return Results.Created($"/api/admin/fleet/maintenance/{id}/tasks", new
            { id = await service.AddMaintenanceTaskAsync(id, request.TemplateId, request.CustomTitleVi, ct) });
        }).AddEndpointFilter<SecureMutationFilter>();
        fleet.MapPut("/tasks/{id:long}", async (long id, SetMaintenanceTaskRequest request, HttpContext context,
            CatalogFleetCommandService service, CancellationToken ct) =>
        {
            CatalogFleetSetupService.Validate(request);
            await service.SetMaintenanceTaskStatusAsync(id, request.Status, request.Note, request.SkipReason, ct, Version(context));
            return Results.NoContent();
        }).AddEndpointFilter<SecureMutationFilter>();
        fleet.MapPost("/maintenance/{id:long}/complete", async (long id, HttpContext context,
            CatalogFleetCommandService service, CancellationToken ct) =>
        {
            await service.CompleteMaintenanceAsync(id, ct, Version(context));
            return Results.NoContent();
        }).RequireAuthorization(AdminIdentityServices.ApproveFleet).AddEndpointFilter<SecureMutationFilter>();
    }

    private static int Skip(int? page)
    {
        if (page is < 1 or > 100000) throw new ValidationException("pagination.invalid_page");
        return ((page ?? 1) - 1) * 50;
    }

    private static byte[] Version(HttpContext context)
    {
        var value = context.Request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(value)) throw new PreconditionRequiredException();
        if (value.Length < 3 || value[0] != '"' || value[^1] != '"') throw new ValidationException("concurrency.invalid_etag");
        try
        {
            var result = Convert.FromBase64String(value[1..^1]);
            return result.Length == 8 ? result : throw new ValidationException("concurrency.invalid_etag");
        }
        catch (FormatException) { throw new ValidationException("concurrency.invalid_etag"); }
    }

    internal static IResult Problem(int status, string code) => Results.Problem(statusCode: status,
        title: code, extensions: new Dictionary<string, object?> { ["code"] = code });
}

public sealed record LoginRequest(string Username, string Password);
public sealed class PreconditionRequiredException : Exception;

public sealed class SecureMutationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var context = invocation.HttpContext;
        if (!context.Request.IsHttps) return AdminEndpoints.Problem(400, "auth.https_required");
        try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return AdminEndpoints.Problem(400, "auth.csrf_invalid"); }
        context.Response.Headers.CacheControl = "no-store";
        return await next(invocation);
    }
}
