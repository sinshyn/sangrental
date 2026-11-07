using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace SangRental.Infrastructure.Persistence;
internal sealed class DatabaseReadinessCheck(SangRentalDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database is unavailable.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Database is unavailable.");
        }
    }
}
