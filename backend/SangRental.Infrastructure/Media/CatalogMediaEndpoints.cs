using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SangRental.Application.Media;
using SangRental.Domain.CatalogFleet;
using SangRental.Infrastructure.Persistence;

namespace SangRental.Infrastructure.Media;

public static class CatalogMediaEndpoints
{
    public static void MapCatalogMediaEndpoints(this IEndpointRouteBuilder endpoints, bool enableUiRedirects = false)
    {
        var admin = endpoints.MapGroup("/api/admin/catalog/models/{modelId:long}/images").RequireAuthorization("AdminAccess");
        admin.MapGet("", async (long modelId, HttpContext context, SangRentalDbContext db, int? page, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "private, no-store";
            if (page is < 1 or > 100000) return Error(400, "request.invalid");
            var images = await db.Set<VehicleModelImage>().AsNoTracking().Where(x => x.VehicleModelId == modelId)
                .OrderByDescending(x => x.Id).Skip(((page ?? 1) - 1) * 50).Take(50)
                .Select(x => new CatalogImageSummary(x.Id, x.MediaType, x.Width, x.Height, x.ApprovalStatus, x.IsVisible,
                    x.IsPrimary, x.RetiredAtUtc != null, Convert.ToBase64String(x.RowVersion),
                    db.Set<VehicleModelImageTranslation>().Where(t => t.VehicleModelImageId == x.Id && t.Locale == "vi").Select(t => t.AltText).FirstOrDefault() ?? ""))
                .ToListAsync(ct);
            return Results.Ok(images);
        });
        admin.MapGet("/{imageId:long}/content", async (long modelId, long imageId, HttpContext context, SangRentalDbContext db, IMediaStorage storage, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "private, no-store";
            var image = await db.Set<VehicleModelImage>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == imageId && x.VehicleModelId == modelId, ct);
            return image is null ? Results.NotFound() : Content(context, storage, image, false);
        });
        admin.MapPost("", (long modelId, HttpContext context) => MutateAsync(context, modelId, null, null, enableUiRedirects));
        admin.MapPost("/{imageId:long}/{action}", (long modelId, long imageId, string action, HttpContext context) =>
            MutateAsync(context, modelId, imageId, action, enableUiRedirects));

        endpoints.MapGet("/media/catalog/{imageId:long}/{fileName}", async (long imageId, string fileName, HttpContext context,
            SangRentalDbContext db, IMediaStorage storage, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var image = await (from i in db.Set<VehicleModelImage>().AsNoTracking()
                join m in db.Set<VehicleModel>() on i.VehicleModelId equals m.Id
                join c in db.Set<VehicleCategory>() on m.VehicleCategoryId equals c.Id
                join b in db.Set<VehicleBrand>() on m.VehicleBrandId equals b.Id
                where i.Id == imageId && i.IsVisible && i.ApprovalStatus == "Approved" && i.RetiredAtUtc == null
                    && i.ApprovedAtUtc != null && i.ApprovedByReference != null && i.SourceReference != null && i.RightsReference != null
                    && m.LifecycleStatus == "Active" && m.PublicationStatus == "Published"
                    && c.LifecycleStatus == "Active" && c.PublicationStatus == "Published" && b.LifecycleStatus == "Active"
                select i).SingleOrDefaultAsync(ct);
            if (image is null || Path.GetFileName(image.StorageKey) != fileName) return Results.NotFound();
            return Content(context, storage, image, true);
        }).AllowAnonymous();
    }

    private static async Task<IResult> MutateAsync(HttpContext context, long modelId, long? imageId, string? action, bool enableUiRedirects)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        if (!context.Request.IsHttps) return Error(400, "auth.https_required");
        var settings = context.RequestServices.GetRequiredService<IOptions<MediaOptions>>().Value;
        var html = enableUiRedirects && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
        if (!settings.Enabled) return html ? Redirect(context, modelId, "media.disabled") : Error(503, "media.disabled");
        var ui = false;
        try
        {
            var limit = settings.MaxUploadBytes + 64 * 1024;
            if (context.Request.ContentLength > limit) return html ? Redirect(context, modelId, "media.too_large") : Error(413, "media.too_large");
            var size = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (size is { IsReadOnly: false }) size.MaxRequestBodySize = limit;
            if (context.Request.HasFormContentType)
            {
                context.Request.EnableBuffering(64 * 1024, limit);
                context.Features.Set<IFormFeature>(new FormFeature(context.Request, new FormOptions
                {
                    MultipartBodyLengthLimit = settings.MaxUploadBytes,
                    ValueLengthLimit = 2000, ValueCountLimit = 10, MultipartHeadersLengthLimit = 4096
                }));
            }
            await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
            IFormCollection? form = context.Request.HasFormContentType ? await context.Request.ReadFormAsync(context.RequestAborted) : null;
            ui = enableUiRedirects && form?["ui"].ToString() == "1";
            var service = context.RequestServices.GetRequiredService<CatalogImageService>();
            if (imageId is null)
            {
                if (form is null || form.Files.Count != 1 || form.Files[0].Name != "file") throw new MediaValidationException("media.file_required");
                await using var input = form.Files[0].OpenReadStream();
                var id = await service.UploadAsync(modelId, input, form.Files[0].FileName,
                    new(form["source"].ToString(), form["rights"].ToString(), form["altText"].ToString()), context.RequestAborted);
                return ui ? Redirect(context, modelId, "saved") : Results.Created($"/api/admin/catalog/models/{modelId}/images", new { id });
            }
            var value = form?["rowVersion"].ToString();
            if (string.IsNullOrEmpty(value))
            {
                var header = context.Request.Headers.IfMatch.ToString();
                if (header.Length == 0) return Error(428, "concurrency.if_match_required");
                if (header.Length < 3 || header[0] != '"' || header[^1] != '"') throw new ValidationException();
                value = header[1..^1];
            }
            if (value.Length > 24) throw new ValidationException();
            byte[] version;
            try { version = Convert.FromBase64String(value); } catch (FormatException) { throw new ValidationException(); }
            await service.ChangeAsync(modelId, imageId.Value, action!, version, context.RequestAborted);
            return ui ? Redirect(context, modelId, "saved") : Results.NoContent();
        }
        catch (AntiforgeryValidationException) { return Error(400, "auth.csrf_invalid"); }
        catch (MediaValidationException e)
        {
            var status = e.Code switch
            {
                "media.not_found" => 404, "media.too_large" => 413, "media.disabled" => 503,
                "media.retired" or "media.unchanged" or "media.not_approved" or "media.model_unavailable" => 409,
                _ => 400
            };
            return ui ? Redirect(context, modelId, e.Code) : Error(status, e.Code);
        }
        catch (ValidationException) { return ui ? Redirect(context, modelId, "request.invalid") : Error(400, "request.invalid"); }
        catch (DbUpdateConcurrencyException) { return ui ? Redirect(context, modelId, "concurrency.resource_changed") : Error(412, "concurrency.resource_changed"); }
        catch (InvalidDataException) { return html ? Redirect(context, modelId, "media.too_large") : Error(413, "media.too_large_or_invalid_form"); }
        catch (IOException) { return ui ? Redirect(context, modelId, "media.storage_unavailable") : Error(503, "media.storage_unavailable"); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return ui ? Redirect(context, modelId, "media.unavailable") : Error(503, "media.unavailable");
        }
    }

    private static IResult Content(HttpContext context, IMediaStorage storage, VehicleModelImage image, bool isPublic)
    {
        try
        {
            var stream = storage.OpenCatalogRead(image.StorageKey);
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers.CacheControl = isPublic ? "public, max-age=31536000, immutable" : "private, no-store";
            return Results.Stream(stream, image.MediaType, entityTag: isPublic ? new EntityTagHeaderValue('"' + Convert.ToHexString(image.ContentHash) + '"') : null,
                enableRangeProcessing: isPublic);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException or MediaValidationException)
        { context.Response.Headers.CacheControl = "no-store"; return Results.NotFound(); }
    }
    private static IResult Redirect(HttpContext context, long modelId, string notice)
    {
        var protection = context.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("CatalogMediaNotice.v1");
        context.Response.Cookies.Append("SangRental.MediaNotice", protection.Protect(notice), new CookieOptions
        { HttpOnly = true, Secure = true, SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict, Path = $"/catalog/models/{modelId}/images", MaxAge = TimeSpan.FromMinutes(1) });
        return Results.LocalRedirect($"/catalog/models/{modelId}/images");
    }
    private static IResult Error(int status, string code) => Results.Problem(statusCode: status, title: code,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
