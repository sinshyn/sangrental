using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using SangRental.Infrastructure.Security;

namespace SangRental.Admin.Services;

public sealed class AdminRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory, IServiceScopeFactory scopes)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState state, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<AdminUser>>();
        var user = await signIn.ValidateSecurityStampAsync(state.User);
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user)) return false;
        var current = await signIn.CreateUserPrincipalAsync(user);
        static IEnumerable<string> Capabilities(ClaimsPrincipal principal) => principal.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == AdminIdentityServices.PermissionClaim)
            .Select(c => c.Type + ":" + c.Value).Order(StringComparer.Ordinal);
        return Capabilities(state.User).SequenceEqual(Capabilities(current));
    }
}
