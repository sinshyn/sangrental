using Microsoft.AspNetCore.Identity;

namespace SangRental.Infrastructure.Security;

public sealed class AdminUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
}

public sealed class AdminSecurityEvent
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string ActionCode { get; set; } = null!;
    public DateTime OccurredAtUtc { get; set; }
}
