namespace SangRental.Infrastructure.Media;

public sealed class MediaOptions
{
    public bool Enabled { get; set; } = true;
    // Both API/Admin projects are two levels below the repository root in Development.
    public string RootPath { get; set; } = "../../artifacts/media";
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
    public long MaxPixels { get; set; } = 20_000_000;
    public int MaxDimension { get; set; } = 10_000;
    public string ContentFormat { get; set; } = "webp";
    public int Quality { get; set; } = 82;
}
