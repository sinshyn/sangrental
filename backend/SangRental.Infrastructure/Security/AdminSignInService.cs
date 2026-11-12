using Microsoft.AspNetCore.Identity;
using SangRental.Infrastructure.Persistence;

namespace SangRental.Infrastructure.Security;

public sealed class AdminSignInService(
    UserManager<AdminUser> users, SignInManager<AdminUser> signIn, SangRentalDbContext db)
{
    private static readonly PasswordHasher<AdminUser> DummyHasher = new();
    private static readonly AdminUser DummyUser = new();
    private static readonly string DummyHash = DummyHasher.HashPassword(DummyUser, Guid.NewGuid().ToString());

    public async Task<bool> SignInAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 256
            || string.IsNullOrEmpty(password) || password.Length > 256) return false;
        var user = await users.FindByNameAsync(username.Trim());
        if (user is null)
        {
            _ = DummyHasher.VerifyHashedPassword(DummyUser, DummyHash, password);
            await AuditAsync(null, "LoginRejected", cancellationToken);
            return false;
        }
        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        var permitted = user.IsActive && result.Succeeded && !user.TwoFactorEnabled
            && (await users.IsInRoleAsync(user, "Admin") || await users.IsInRoleAsync(user, "Manager")
                || await users.IsInRoleAsync(user, "Staff"));
        await AuditAsync(user.Id, permitted ? "LoginSucceeded" : "LoginRejected", cancellationToken);
        if (!permitted) return false;
        await signIn.SignInAsync(user, isPersistent: false);
        return true;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(signIn.Context.User);
        // Revoke all existing sessions, including a copied cookie, before clearing this browser cookie.
        if (user is not null)
        {
            var result = await users.UpdateSecurityStampAsync(user);
            if (!result.Succeeded) throw new InvalidOperationException("Session revocation failed.");
            await AuditAsync(user.Id, "SessionsRevoked", cancellationToken);
        }
        await signIn.SignOutAsync();
    }

    private async Task AuditAsync(Guid? userId, string action, CancellationToken cancellationToken)
    {
        db.Set<AdminSecurityEvent>().Add(new AdminSecurityEvent { UserId = userId, ActionCode = action });
        await db.SaveChangesAsync(cancellationToken);
    }
}
