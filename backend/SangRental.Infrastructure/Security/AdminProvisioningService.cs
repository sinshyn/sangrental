using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SangRental.Infrastructure.Persistence;

namespace SangRental.Infrastructure.Security;

public sealed class AdminProvisioningService(
    SangRentalDbContext db, UserManager<AdminUser> users, RoleManager<IdentityRole<Guid>> roles)
{
    public async Task ProvisionFirstAdminAsync(string username, string password, CancellationToken cancellationToken = default)
        => await ProvisionAsync(username, password, false, cancellationToken);

    public async Task ProvisionLocalTestAdminAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.Database != "SangRentalTestDb" || connection.DataSource is not ("tcp:127.0.0.1,1436" or "127.0.0.1,1436"))
            throw new InvalidOperationException("Additional test administrators are allowed only in the local test database.");
        await ProvisionAsync(username, password, true, cancellationToken);
    }

    private async Task ProvisionAsync(string username, string password, bool testOnly, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await SqlApplicationLock.AcquireAsync(db, "SangRental:Identity:FirstAdmin", cancellationToken);
        if (!testOnly && await users.GetUsersInRoleAsync("Admin") is { Count: > 0 })
            throw new InvalidOperationException("An administrator already exists; bootstrap is disabled.");
        foreach (var role in new[] { "Admin", "Manager", "Staff" })
            if (!await roles.RoleExistsAsync(role)) Ensure(await roles.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() }));
        var user = new AdminUser { Id = Guid.NewGuid(), UserName = username.Trim(), IsActive = true };
        Ensure(await users.CreateAsync(user, password));
        Ensure(await users.AddToRoleAsync(user, "Admin"));
        db.Set<AdminSecurityEvent>().Add(new AdminSecurityEvent { UserId = user.Id,
            ActionCode = testOnly ? "LocalTestAdminProvisioned" : "FirstAdminProvisioned" });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Identity rejected the operation: " + string.Join(", ", result.Errors.Select(e => e.Code)));
    }
}
