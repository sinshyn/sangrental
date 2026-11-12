using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SangRental.Application.Media;

namespace SangRental.Infrastructure.Media;

public sealed class LocalMediaStorage : IMediaStorage
{
    private readonly string root;
    private readonly IDataProtector protector;
    private readonly TimeProvider clock;
    public LocalMediaStorage(IOptions<MediaOptions> options, IHostEnvironment environment,
        IDataProtectionProvider protection, TimeProvider clock)
    {
        var configured = options.Value.RootPath;
        if (!environment.IsDevelopment() && !Path.IsPathFullyQualified(configured))
            throw new InvalidOperationException("Configure an absolute durable Media:RootPath outside web roots.");
        root = Path.GetFullPath(configured, environment.ContentRootPath);
        var webRoot = Path.GetFullPath("wwwroot", environment.ContentRootPath);
        if (root.Equals(webRoot, StringComparison.OrdinalIgnoreCase)
            || root.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Media storage must be outside wwwroot.");
        protector = protection.CreateProtector("SangRental.PrivateOriginalMedia.v1");
        this.clock = clock;
    }

    public async Task<StoredMedia> SaveNewAsync(PreparedMedia media, CancellationToken ct = default)
    {
        var prefix = media.Purpose == MediaPurpose.Catalog ? "catalog" : "private/" + media.Purpose;
        var uploadedAt = clock.GetUtcNow();
        var date = uploadedAt.ToString("yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture);
        var payload = media.Purpose == MediaPurpose.Catalog ? media.Bytes : protector.Protect(media.Bytes);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var key = $"{prefix}/{date}/{Guid.CreateVersion7(uploadedAt):N}{media.Extension}";
            var path = Resolve(key, media.Purpose == MediaPurpose.Catalog);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var created = false;
            try
            {
                await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                {
                    created = true;
                    await stream.WriteAsync(payload, ct);
                    await stream.FlushAsync(ct);
                }
                return new(key, SHA256.HashData(media.Bytes), media.MediaType, media.Width, media.Height);
            }
            catch (IOException) when (!created && File.Exists(path)) { /* Generate a different UUID; never overwrite. */ }
            catch
            {
                if (created) File.Delete(path);
                throw;
            }
        }
        throw new IOException("Could not allocate a new media file.");
    }

    public Stream OpenCatalogRead(string key) => new FileStream(Resolve(key, true), FileMode.Open, FileAccess.Read, FileShare.Read);
    // Internal primitive only: future document aggregate must authorize and audit before calling.
    public byte[] ReadPrivateOriginal(string key) => protector.Unprotect(File.ReadAllBytes(Resolve(key, false)));
    public void DeleteUncommitted(StoredMedia media)
    {
        var path = Resolve(media.StorageKey, media.StorageKey.StartsWith("catalog/", StringComparison.Ordinal));
        File.Delete(path);
    }

    private string Resolve(string key, bool catalog)
    {
        var prefix = catalog ? "catalog" : "private/(?:IdentityDocument|DrivingLicense|Document|HandoverEvidence)";
        if (!Regex.IsMatch(key, "^" + prefix + @"/\d{4}/\d{2}/\d{2}/[a-f0-9]{32}\.(?:webp|jpg|jpeg|png|gif|bmp)$", RegexOptions.CultureInvariant))
            throw new MediaValidationException("media.invalid_storage_key");
        return Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
    }
}
