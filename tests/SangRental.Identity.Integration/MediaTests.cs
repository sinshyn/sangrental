using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SangRental.Application.Media;
using SangRental.Application.CatalogFleet;
using Microsoft.Extensions.Logging.Abstractions;
using SangRental.Domain.CatalogFleet;
using SangRental.Infrastructure.Media;
using SangRental.Infrastructure.Persistence;
using SkiaSharp;

internal static partial class IdentityIntegrationRunner
{
    private static byte[] TestImage(SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        using var bitmap = new SKBitmap(2, 3);
        bitmap.Erase(SKColors.Transparent);
        bitmap.SetPixel(0, 0, SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        return data.ToArray();
    }
    private static byte[] WithOrientation(byte[] jpeg)
    {
        byte[] exif = [0xff,0xe1,0,0x22,0x45,0x78,0x69,0x66,0,0,0x49,0x49,0x2a,0,8,0,0,0,1,0,0x12,1,3,0,1,0,0,0,6,0,0,0,0,0,0,0];
        return [..jpeg.Take(2), ..exif, ..jpeg.Skip(2)];
    }
    private static async Task VerifyMediaPrimitivesAsync()
    {
        var directory = Path.GetFullPath("artifacts/media-tests/" + Guid.NewGuid().ToString("N"));
        var settings = new MediaOptions { RootPath = directory };
        var protection = new ServiceCollection().AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(directory + "/keys"));
        if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        using var provider = protection.Services.BuildServiceProvider();
        var processor = new ImageProcessingService(Options.Create(settings));
        var storage = new LocalMediaStorage(Options.Create(settings), new MediaEnvironment(), provider.GetRequiredService<IDataProtectionProvider>(), TimeProvider.System);
        var png = TestImage();
        var prepared = await processor.PrepareAsync(new MemoryStream(png), "same-name.png", MediaPurpose.Catalog);
        using (var data = SKData.CreateCopy(prepared.Bytes))
        using (var codec = SKCodec.Create(data)) Check(codec.EncodedFormat == SKEncodedImageFormat.Webp, "Output really is WebP");
        using (var decoded = SKBitmap.Decode(prepared.Bytes)) Check(decoded.GetPixel(1, 2).Alpha == 0, "WebP preserves transparency");
        Check(prepared.Width == 2 && prepared.Height == 3 && prepared.Extension == ".webp", "Output metadata matches image");
        var saved = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => storage.SaveNewAsync(prepared)));
        Check(saved.Select(x => x.StorageKey).Distinct().Count() == 8, "Concurrent identical uploads have distinct storage keys");
        Check(saved.All(x => Path.GetFileNameWithoutExtension(x.StorageKey)[12] == '7'), "Stored file IDs are UUID v7");
        Check(saved.All(x => x.ContentHash.SequenceEqual(SHA256.HashData(prepared.Bytes))), "Hash describes output bytes without deduplication");
        using (var file = storage.OpenCatalogRead(saved[0].StorageKey))
        using (var result = new MemoryStream()) { await file.CopyToAsync(result); Check(result.ToArray().SequenceEqual(prepared.Bytes), "Stored bytes equal processed bytes"); }
        var jpeg = WithOrientation(TestImage(SKEncodedImageFormat.Jpeg));
        var rotated = await processor.PrepareAsync(new MemoryStream(jpeg), "rotated.jpeg", MediaPurpose.Catalog);
        Check(rotated.Width == 3 && rotated.Height == 2, "EXIF orientation applied to content image");
        Check(!System.Text.Encoding.ASCII.GetString(rotated.Bytes).Contains("Exif"), "Content output strips EXIF");
        foreach (var purpose in new[] { MediaPurpose.IdentityDocument, MediaPurpose.DrivingLicense, MediaPurpose.Document, MediaPurpose.HandoverEvidence })
        {
            var original = await processor.PrepareAsync(new MemoryStream(jpeg), "original.jpeg", purpose);
            Check(original.Bytes.SequenceEqual(jpeg) && original.Extension == ".jpeg" && original.MediaType == "image/jpeg", "Private format and bytes preserved: " + purpose);
            var stored = await storage.SaveNewAsync(original);
            Check(storage.ReadPrivateOriginal(stored.StorageKey).SequenceEqual(jpeg), "Private original decrypts byte-for-byte: " + purpose);
            Check(!File.ReadAllBytes(Path.Combine(directory, stored.StorageKey)).SequenceEqual(jpeg), "Private storage is encrypted: " + purpose);
            await ExpectAsync<MediaValidationException>(() => Task.Run(() => storage.OpenCatalogRead(stored.StorageKey)));
        }
        var jpgProcessor = new ImageProcessingService(Options.Create(new MediaOptions { ContentFormat = "jpg" }));
        var jpg = await jpgProcessor.PrepareAsync(new MemoryStream(png), "alpha.png", MediaPurpose.Catalog);
        using (var codec = SKCodec.Create(SKData.CreateCopy(jpg.Bytes))) Check(codec.EncodedFormat == SKEncodedImageFormat.Jpeg, "JPEG output really is JPEG");
        using (var bitmap = SKBitmap.Decode(jpg.Bytes)) Check(bitmap.GetPixel(1, 2).Alpha == 255, "JPEG has opaque background");
        await ExpectAsync<MediaValidationException>(() => processor.PrepareAsync(new MemoryStream(png), "wrong.jpg", MediaPurpose.Catalog));
        await ExpectAsync<MediaValidationException>(() => processor.PrepareAsync(new MemoryStream([1, 2, 3]), "bad.png", MediaPurpose.Catalog));
        await ExpectAsync<MediaValidationException>(() => new ImageProcessingService(Options.Create(new MediaOptions { MaxUploadBytes = 2 }))
            .PrepareAsync(new MemoryStream(png), "large.png", MediaPurpose.Catalog));
        await ExpectAsync<MediaValidationException>(() => new ImageProcessingService(Options.Create(new MediaOptions { MaxPixels = 1 }))
            .PrepareAsync(new MemoryStream(png), "pixels.png", MediaPurpose.Catalog));
        await ExpectAsync<MediaValidationException>(() => new ImageProcessingService(Options.Create(new MediaOptions { Enabled = false }))
            .PrepareAsync(new MemoryStream(png), "disabled.png", MediaPurpose.Catalog));
        await ExpectAsync<MediaValidationException>(() => Task.Run(() => storage.OpenCatalogRead("../private/secret.jpg")));
    }

    private static async Task VerifyCatalogMediaAsync(HttpClient admin, HttpClient anonymous, HttpClient blazor,
        IServiceProvider services, DbContextOptions<SangRentalDbContext> options, long model, string suffix)
    {
        var path = $"/api/admin/catalog/models/{model}/images";
        var uiPath = $"/catalog/models/{model}/images";
        var imageBytes = TestImage();
        async Task<HttpResponseMessage> Upload(HttpClient client, byte[]? bytes = null, string? token = "auto", bool ui = false)
        {
            var multipart = new MultipartFormDataContent();
            multipart.Add(new ByteArrayContent(bytes ?? imageBytes), "file", "same-name.png");
            multipart.Add(new StringContent("Test fixture"), "source");
            multipart.Add(new StringContent("Generated by test"), "rights");
            multipart.Add(new StringContent("Ảnh kiểm thử"), "altText");
            if (ui) multipart.Add(new StringContent("1"), "ui");
            if (token == "auto") token = (await client.GetFromJsonAsync<JsonElement>("/auth/csrf")).GetProperty("requestToken").GetString();
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = multipart };
            if (ui) request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            if (token is not null) request.Headers.Add("X-CSRF-TOKEN", token);
            return await client.SendAsync(request);
        }
        Status(await anonymous.GetAsync(path), HttpStatusCode.Unauthorized, "Anonymous cannot list model images");
        Status(await Upload(anonymous), HttpStatusCode.Unauthorized, "Anonymous cannot upload images");
        Status(await Upload(admin, token: null), HttpStatusCode.BadRequest, "Image upload requires CSRF");
        var first = await Upload(admin);
        Status(first, HttpStatusCode.Created, "Catalog image uploaded");
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
        var second = await Upload(admin);
        Status(second, HttpStatusCode.Created, "Same image same name uploaded again");
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
        Check(firstId != secondId, "Duplicate content gets a new SQL image row");
        var summaries = (await admin.GetFromJsonAsync<List<CatalogImageSummary>>(path))!;
        Check(summaries.Count == 2 && summaries.All(x => x.ApprovalStatus == "Draft" && !x.IsVisible), "New images stay private drafts");
        var previewPath = path + $"/{firstId}/content";
        Status(await anonymous.GetAsync(previewPath), HttpStatusCode.Unauthorized, "Anonymous preview denied");
        var preview = await admin.GetAsync(previewPath);
        Status(preview, HttpStatusCode.OK, "Admin preview served");
        Check(preview.Content.Headers.ContentType?.MediaType == "image/webp" && preview.Headers.CacheControl?.NoStore == true, "Preview uses real output type and no-store");
        string publicPath;
        await using (var db = new SangRentalDbContext(options))
        {
            var row = await db.Set<VehicleModelImage>().SingleAsync(x => x.Id == firstId);
            var other = await db.Set<VehicleModelImage>().SingleAsync(x => x.Id == secondId);
            Check(row.StorageKey != other.StorageKey && row.ContentHash.SequenceEqual(other.ContentHash), "Distinct files despite equal content hash");
            Check(row.MediaType == "image/webp" && row.Width == 2 && row.Height == 3, "Image SQL metadata matches output");
            Check(await db.Set<CatalogFleetAuditEvent>().AnyAsync(x => x.EntityType == "VehicleModelImage" && x.EntityId == firstId && x.ActionCode == "Uploaded"), "Image upload audited");
            publicPath = $"/media/catalog/{row.Id}/{Path.GetFileName(row.StorageKey)}";
        }
        var draftPublic = await anonymous.GetAsync(publicPath);
        Status(draftPublic, HttpStatusCode.NotFound, "Draft public URL denied");
        Check(draftPublic.Headers.CacheControl?.NoStore == true, "Denied media is not cached");
        var version = summaries.Single(x => x.Id == firstId).RowVersion;
        Status(await SendAsync(admin, HttpMethod.Post, path + $"/{firstId}/approve", null), (HttpStatusCode)428, "Image approval needs version");
        Status(await SendAsync(admin, HttpMethod.Post, path + $"/{firstId}/approve", null, version), HttpStatusCode.NoContent, "Image rights approved");
        Status(await SendAsync(admin, HttpMethod.Post, path + $"/{firstId}/hide", null, version), HttpStatusCode.PreconditionFailed, "Stale image version denied");
        Status(await anonymous.GetAsync(publicPath), HttpStatusCode.NotFound, "Approved image still not public while model draft");
        // Simulate approved publication setup only on the test DB; not a new publication API.
        await using (var db = new SangRentalDbContext(options))
        {
            var m = await db.Set<VehicleModel>().SingleAsync(x => x.Id == model);
            var category = await db.Set<VehicleCategory>().SingleAsync(x => x.Id == m.VehicleCategoryId);
            m.PublicationStatus = "Published"; category.PublicationStatus = "Published";
            await db.SaveChangesAsync();
        }
        var published = await anonymous.GetAsync(publicPath);
        Status(published, HttpStatusCode.OK, "Published catalog image served anonymously");
        Check(published.Headers.CacheControl?.Public == true && published.Headers.CacheControl?.MaxAge == TimeSpan.FromDays(365)
            && published.Headers.CacheControl.ToString().Contains("immutable"), "Public media has immutable year cache");
        Check(published.Headers.ETag != null, "Public media has ETag");
        using (var conditional = new HttpRequestMessage(HttpMethod.Get, publicPath))
        {
            conditional.Headers.IfNoneMatch.Add(published.Headers.ETag!);
            Status(await anonymous.SendAsync(conditional), HttpStatusCode.NotModified, "ETag revalidation returns 304");
        }
        summaries = (await admin.GetFromJsonAsync<List<CatalogImageSummary>>(path))!;
        Status(await SendAsync(admin, HttpMethod.Post, path + $"/{firstId}/primary", null, summaries.Single(x => x.Id == firstId).RowVersion),
            HttpStatusCode.NoContent, "Approved image becomes primary");
        summaries = (await admin.GetFromJsonAsync<List<CatalogImageSummary>>(path))!;
        Status(await SendAsync(admin, HttpMethod.Post, path + $"/{secondId}/primary", null, summaries.Single(x => x.Id == secondId).RowVersion),
            HttpStatusCode.Conflict, "Draft cannot become primary");
        Status(await SendAsync(admin, HttpMethod.Post, path + $"/{secondId}/approve", null, summaries.Single(x => x.Id == secondId).RowVersion),
            HttpStatusCode.NoContent, "Second image approved");
        summaries = (await admin.GetFromJsonAsync<List<CatalogImageSummary>>(path))!;
        var primaryRace = await Task.WhenAll(
            SendAsync(admin, HttpMethod.Post, path + $"/{secondId}/primary", null, summaries.Single(x => x.Id == secondId).RowVersion),
            SendAsync(admin, HttpMethod.Post, path + $"/{secondId}/primary", null, summaries.Single(x => x.Id == secondId).RowVersion));
        Check(primaryRace.Count(x => x.StatusCode == HttpStatusCode.NoContent) == 1
            && primaryRace.Count(x => x.StatusCode == HttpStatusCode.PreconditionFailed) == 1, "Concurrent primary switch succeeds once");
        summaries = (await admin.GetFromJsonAsync<List<CatalogImageSummary>>(path))!;
        Check(summaries.Count(x => x.IsPrimary) == 1 && summaries.Single(x => x.IsPrimary).Id == secondId, "Filtered primary index and old primary preserved correctly");
        var invalid = await Upload(admin, [1, 2, 3]);
        Status(invalid, HttpStatusCode.BadRequest, "Broken image rejected");
        Check((await admin.GetFromJsonAsync<List<CatalogImageSummary>>(path))!.Count == 2, "Rejected upload creates no metadata");
        Status(await Upload(admin, new byte[10 * 1024 * 1024 + 65537]), HttpStatusCode.RequestEntityTooLarge, "Oversized multipart rejected");
        await using (var scope = services.CreateAsyncScope())
        {
            var storage = scope.ServiceProvider.GetRequiredService<IMediaStorage>();
            var capture = new CapturingStorage(storage);
            var service = new CatalogImageService(scope.ServiceProvider.GetRequiredService<SangRentalDbContext>(),
                new MediaActor(), new InvalidMetadataProcessor(), capture, TimeProvider.System, NullLogger<CatalogImageService>.Instance);
            await ExpectAsync<DbUpdateException>(() => service.UploadAsync(model, new MemoryStream(imageBytes), "same-name.png", new("Fixture", "Fixture rights", "Fixture"), default));
            Check(capture.Cleaned && capture.Saved != null, "DB failure cleans only newly stored file");
            await ExpectAsync<FileNotFoundException>(() => Task.Run(() => storage.OpenCatalogRead(capture.Saved!.StorageKey)));
        }
        var html = await blazor.GetStringAsync(uiPath);
        Check(html.Contains("multipart/form-data") && html.Contains("name=\"file\""), "Admin image page has native multipart form");
        var formToken = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Check(formToken.Success, "Image page has antiforgery token");
        Status(await Upload(blazor, new byte[10 * 1024 * 1024 + 65537], WebUtility.HtmlDecode(formToken.Groups[1].Value), ui: true),
            HttpStatusCode.Redirect, "Oversized native upload returns to Admin page");
        Check(WebUtility.HtmlDecode(await blazor.GetStringAsync(uiPath)).Contains("Ảnh vượt giới hạn"), "Oversized upload validation is inline");
        Status(await Upload(blazor, token: WebUtility.HtmlDecode(formToken.Groups[1].Value), ui: true), HttpStatusCode.Redirect, "Admin native upload redirects after commit");
        Check((await blazor.GetStringAsync(uiPath)).Contains("Ảnh #"), "Admin displays persisted images");
        summaries = (await admin.GetFromJsonAsync<List<CatalogImageSummary>>(path))!;
        var current = summaries.Single(x => x.Id == firstId);
        Status(await SendAsync(admin, HttpMethod.Post, path + $"/{firstId}/retire", null, current.RowVersion), HttpStatusCode.NoContent, "Retire keeps image history");
        Status(await anonymous.GetAsync(publicPath), HttpStatusCode.NotFound, "Retired image origin no longer public");
        await using (var db = new SangRentalDbContext(options))
        {
            var row = await db.Set<VehicleModelImage>().SingleAsync(x => x.Id == firstId);
            using var stream = services.GetRequiredService<IMediaStorage>().OpenCatalogRead(row.StorageKey);
            Check(stream.Length > 0 && row.RetiredAtUtc != null, "Retirement preserves immutable file");
            var m = await db.Set<VehicleModel>().SingleAsync(x => x.Id == model);
            var category = await db.Set<VehicleCategory>().SingleAsync(x => x.Id == m.VehicleCategoryId);
            m.PublicationStatus = "Draft"; category.PublicationStatus = "Draft";
            await db.SaveChangesAsync();
        }
    }
    private sealed class MediaEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "MediaTests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private sealed class MediaActor : ICatalogFleetActorSource
    {
        public CatalogFleetActor GetRequiredActor() => new("media-rollback-test", true, false, false, false);
    }
    private sealed class InvalidMetadataProcessor : IImageProcessingService
    {
        public Task<PreparedMedia> PrepareAsync(Stream input, string name, MediaPurpose purpose, CancellationToken ct = default)
            => Task.FromResult(new PreparedMedia(TestImage(), ".webp", "image/webp", 0, 3, purpose));
    }
    private sealed class CapturingStorage(IMediaStorage storage) : IMediaStorage
    {
        public StoredMedia? Saved;
        public bool Cleaned;
        public async Task<StoredMedia> SaveNewAsync(PreparedMedia media, CancellationToken ct = default) => Saved = await storage.SaveNewAsync(media, ct);
        public Stream OpenCatalogRead(string key) => storage.OpenCatalogRead(key);
        public byte[] ReadPrivateOriginal(string key) => storage.ReadPrivateOriginal(key);
        public void DeleteUncommitted(StoredMedia media) { storage.DeleteUncommitted(media); Cleaned = true; }
    }
}
