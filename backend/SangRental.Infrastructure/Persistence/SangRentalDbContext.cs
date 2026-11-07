using Microsoft.EntityFrameworkCore;
namespace SangRental.Infrastructure.Persistence;
public sealed class SangRentalDbContext(DbContextOptions<SangRentalDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("sangrental");
        // Add entity configurations when the first module schema is reviewed.
    }
}
