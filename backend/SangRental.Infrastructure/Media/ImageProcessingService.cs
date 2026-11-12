using Microsoft.Extensions.Options;
using SangRental.Application.Media;
using SkiaSharp;

namespace SangRental.Infrastructure.Media;

public sealed class ImageProcessingService(IOptions<MediaOptions> options) : IImageProcessingService
{
    private readonly SemaphoreSlim gate = new(2, 2);

    public async Task<PreparedMedia> PrepareAsync(Stream input, string originalName, MediaPurpose purpose, CancellationToken ct = default)
    {
        var settings = options.Value;
        if (!settings.Enabled) throw new MediaValidationException("media.disabled");
        if (!Enum.IsDefined(purpose)) throw new MediaValidationException("media.invalid_purpose");
        await gate.WaitAsync(ct);
        try
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await input.ReadAsync(chunk, ct)) > 0)
            {
                if (buffer.Length + read > settings.MaxUploadBytes) throw new MediaValidationException("media.too_large");
                buffer.Write(chunk, 0, read);
            }
            var bytes = buffer.ToArray();
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec is null) throw new MediaValidationException("media.invalid_image");
            var (type, extensions) = codec.EncodedFormat switch
            {
                SKEncodedImageFormat.Jpeg => ("image/jpeg", new[] { ".jpg", ".jpeg" }),
                SKEncodedImageFormat.Png => ("image/png", new[] { ".png" }),
                SKEncodedImageFormat.Webp => ("image/webp", new[] { ".webp" }),
                SKEncodedImageFormat.Gif => ("image/gif", new[] { ".gif" }),
                SKEncodedImageFormat.Bmp => ("image/bmp", new[] { ".bmp" }),
                _ => throw new MediaValidationException("media.unsupported_format")
            };
            var extension = Path.GetExtension(originalName).ToLowerInvariant();
            if (!extensions.Contains(extension)) throw new MediaValidationException("media.extension_mismatch");
            var width = codec.Info.Width;
            var height = codec.Info.Height;
            if (width <= 0 || height <= 0 || width > settings.MaxDimension || height > settings.MaxDimension
                || (long)width * height > settings.MaxPixels) throw new MediaValidationException("media.too_many_pixels");
            if (purpose == MediaPurpose.Catalog && codec.FrameCount > 1)
                throw new MediaValidationException("media.animated_content_not_supported");
            using var decoded = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success)
                throw new MediaValidationException("media.invalid_image");
            ct.ThrowIfCancellationRequested();
            // Private documents/evidence are inspected only: return original bytes, extension, EXIF and dimensions.
            if (purpose != MediaPurpose.Catalog) return new(bytes, extension, type, width, height, purpose);

            var rotated = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            using var oriented = new SKBitmap(rotated ? height : width, rotated ? width : height);
            using (var canvas = new SKCanvas(oriented))
            {
                canvas.Clear(settings.ContentFormat == "jpg" ? SKColors.White : SKColors.Transparent);
                var matrix = codec.EncodedOrigin switch
                {
                    SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
                    SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
                    SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
                    SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
                    SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),
                    SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1),
                    SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),
                    _ => SKMatrix.Identity
                };
                canvas.SetMatrix(matrix);
                canvas.DrawBitmap(decoded, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
            }
            using var image = SKImage.FromBitmap(oriented);
            using var encoded = image.Encode(settings.ContentFormat == "jpg" ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Webp, settings.Quality)
                ?? throw new MediaValidationException("media.encode_failed");
            return new(encoded.ToArray(), settings.ContentFormat == "jpg" ? ".jpg" : ".webp",
                settings.ContentFormat == "jpg" ? "image/jpeg" : "image/webp", oriented.Width, oriented.Height, purpose);
        }
        finally { gate.Release(); }
    }
}
