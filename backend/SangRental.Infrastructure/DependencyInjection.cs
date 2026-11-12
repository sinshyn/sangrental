using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SangRental.Infrastructure.Persistence;
using SangRental.Application.Media;
using SangRental.Infrastructure.Media;
namespace SangRental.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MediaOptions>().Bind(configuration.GetSection("Media"))
            .Validate(o => o.MaxUploadBytes is > 0 and <= 25 * 1024 * 1024 && o.MaxPixels is > 0 and <= 40_000_000
                && o.MaxDimension is > 0 and <= 16_000 && o.Quality is >= 1 and <= 100
                && o.ContentFormat is "webp" or "jpg" && !string.IsNullOrWhiteSpace(o.RootPath), "Invalid media configuration.");
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();
        services.AddSingleton<IMediaStorage, LocalMediaStorage>();
        services.AddScoped<CatalogImageService>();
        var connectionString = configuration.GetConnectionString("SangRental");
        if (!string.IsNullOrWhiteSpace(connectionString)
            && bool.TryParse(configuration["SqlServer:UseIPv4Loopback"], out var useIPv4Loopback) && useIPv4Loopback)
        {
            // Development-only opt-in: Docker publishes 127.0.0.1, while localhost may select ::1.
            // Match only the approved local endpoint; never rewrite another server or TLS settings.
            var connection = new SqlConnectionStringBuilder(connectionString);
            if (string.Equals(connection.DataSource, "localhost,1436", StringComparison.OrdinalIgnoreCase))
                connection.DataSource = "tcp:127.0.0.1,1436";
            connectionString = connection.ConnectionString;
        }
        services.AddDbContext<SangRentalDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
                options.UseSqlServer(connectionString, sql => sql.CommandTimeout(15));
            else
                options.UseSqlServer(); // Design-time model generation needs a provider, not credentials.
        });
        var checks = services.AddHealthChecks();
        if (string.IsNullOrWhiteSpace(connectionString))
            checks.AddCheck("sql-server", () => HealthCheckResult.Unhealthy("Database is not configured."), tags: ["ready"]);
        else
            checks.AddCheck<DatabaseReadinessCheck>("sql-server", tags: ["ready"]);
        return services;
    }
}
