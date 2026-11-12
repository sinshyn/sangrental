using System.ComponentModel.DataAnnotations;

namespace SangRental.Application.Media;

public enum MediaPurpose { Catalog, IdentityDocument, DrivingLicense, Document, HandoverEvidence }
public sealed record PreparedMedia(byte[] Bytes, string Extension, string MediaType, int Width, int Height, MediaPurpose Purpose);
public sealed record StoredMedia(string StorageKey, byte[] ContentHash, string MediaType, int Width, int Height);
public interface IImageProcessingService
{
    Task<PreparedMedia> PrepareAsync(Stream input, string originalName, MediaPurpose purpose, CancellationToken ct = default);
}
public interface IMediaStorage
{
    Task<StoredMedia> SaveNewAsync(PreparedMedia media, CancellationToken ct = default);
    Stream OpenCatalogRead(string key);
    byte[] ReadPrivateOriginal(string key);
    void DeleteUncommitted(StoredMedia media);
}
public sealed record CatalogImageUpload(
    [property: Required, StringLength(500)] string SourceReference,
    [property: Required, StringLength(500)] string RightsReference,
    [property: Required, StringLength(300)] string AltTextVi);
public sealed record CatalogImageSummary(long Id, string MediaType, int Width, int Height,
    string ApprovalStatus, bool IsVisible, bool IsPrimary, bool IsRetired, string RowVersion, string AltTextVi);
public sealed class MediaValidationException(string code) : Exception(code) { public string Code { get; } = code; }
