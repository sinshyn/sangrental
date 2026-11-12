using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SangRental.Infrastructure.Security;
using SangRental.Domain.CatalogFleet;
namespace SangRental.Infrastructure.Persistence;
public sealed class SangRentalDbContext(DbContextOptions<SangRentalDbContext> options)
    : IdentityDbContext<AdminUser, IdentityRole<Guid>, Guid>(options)
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ProtectHistory();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ProtectHistory();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ProtectHistory()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted
            && entry.Entity is CatalogFleetAuditEvent or VehicleInspection or VehicleOperationalStatusHistory or AdminSecurityEvent))
            throw new InvalidOperationException("History records are append-only through this context.");
        foreach (var entry in ChangeTracker.Entries<VehicleRegistration>())
        {
            if (entry.State == EntityState.Deleted || entry.State == EntityState.Modified
                && (entry.OriginalValues.GetValue<DateTime?>(nameof(VehicleRegistration.ValidToUtc)) is not null
                    || entry.Entity.ValidToUtc is null || entry.Entity.ValidToUtc <= entry.Entity.ValidFromUtc
                    || entry.Entity.ClosedAtUtc is null || string.IsNullOrWhiteSpace(entry.Entity.ClosedByReference)
                    || string.IsNullOrWhiteSpace(entry.Entity.ClosureReason)
                    || entry.Property(x => x.PhysicalVehicleId).IsModified
                    || entry.Property(x => x.RegistrationJurisdictionCode).IsModified
                    || entry.Property(x => x.PlateDisplay).IsModified
                    || entry.Property(x => x.NormalizedPlate).IsModified
                    || entry.Property(x => x.ValidFromUtc).IsModified
                    || entry.Property(x => x.CreatedAtUtc).IsModified
                    || entry.Property(x => x.CreatedByReference).IsModified))
                throw new InvalidOperationException("Registration history can only close a current interval; identity and closed records are immutable.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("sangrental");
        modelBuilder.Entity<AdminUser>().ToTable("Users", "identity");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles", "identity");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "identity");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "identity");
        modelBuilder.Entity<IdentityUserLogin<Guid>>(e =>
        {
            e.ToTable("UserLogins", "identity");
            e.Property(x => x.LoginProvider).HasMaxLength(128);
            e.Property(x => x.ProviderKey).HasMaxLength(128);
        });
        modelBuilder.Entity<IdentityUserToken<Guid>>(e =>
        {
            e.ToTable("UserTokens", "identity");
            e.Property(x => x.LoginProvider).HasMaxLength(128);
            e.Property(x => x.Name).HasMaxLength(128);
        });
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "identity");
        modelBuilder.Entity<AdminSecurityEvent>(e =>
        {
            e.ToTable("SecurityEvents", "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.ActionCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.OccurredAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
        });
        CatalogFleetModelConfiguration.Configure(modelBuilder);
    }
}
