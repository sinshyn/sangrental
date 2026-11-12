namespace SangRental.Application.CatalogFleet;

/// <summary>
/// The implementation must derive these capabilities from a verified server-side identity.
/// The HTTP implementation uses the current Identity principal after security-stamp validation.
/// </summary>
public interface ICatalogFleetActorSource
{
    CatalogFleetActor GetRequiredActor();
}

public sealed record CatalogFleetActor(
    string Reference,
    bool IsAdmin,
    bool CanManageFleet,
    bool CanApproveFleet,
    bool CanPublishCatalog);

public sealed class CatalogFleetRuleException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
