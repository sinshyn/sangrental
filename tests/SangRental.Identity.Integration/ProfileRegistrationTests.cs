using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SangRental.Application.CatalogFleet;
using SangRental.Domain.CatalogFleet;
using SangRental.Infrastructure.Persistence;

internal static partial class IdentityIntegrationRunner
{
    private static async Task VerifyProfileRegistrationAsync(HttpClient admin, HttpClient blazor,
        DbContextOptions<SangRentalDbContext> options, long model, string suffix, Guid actor)
    {
        var first = await CreateAsync(admin, "/api/admin/fleet/vehicles", new { modelId = model, assetCode = "PROFILE-A-" + suffix, confirmInitialInspection = true });
        var second = await CreateAsync(admin, "/api/admin/fleet/vehicles", new { modelId = model, assetCode = "PROFILE-B-" + suffix, confirmInitialInspection = true });
        var path = $"/api/admin/fleet/vehicles/{first}";
        var otherPath = $"/api/admin/fleet/vehicles/{second}";
        async Task<VehicleDetails> Details(string url) => (await admin.GetFromJsonAsync<VehicleDetails>(url))!;
        var original = await Details(path);
        var vin = "QA-VIN-" + suffix;
        var profile = new { modelId = model, colorName = "Test blue", manufacturingYear = 2024, vin, reason = "Correct profile" };
        Status(await SendAsync(admin, HttpMethod.Put, path + "/profile", profile), (HttpStatusCode)428, "Profile needs version");
        Status(await SendAsync(admin, HttpMethod.Put, path + "/profile", new { modelId = long.MaxValue, reason = "Invalid model" }, original.RowVersion),
            HttpStatusCode.Conflict, "Profile rejects unavailable model");
        Status(await SendAsync(admin, HttpMethod.Put, path + "/profile", new { modelId = model, manufacturingYear = -1, reason = "Invalid year" }, original.RowVersion),
            HttpStatusCode.BadRequest, "Profile rejects negative year");
        Status(await SendAsync(admin, HttpMethod.Put, path + "/profile", profile, original.RowVersion), HttpStatusCode.NoContent, "Profile updated");
        var updated = await Details(path);
        Check(updated.ColorName == "Test blue" && updated.ManufacturingYear == 2024 && updated.Vin == vin
            && updated.AssetCode == original.AssetCode && updated.OperationalStatus == original.OperationalStatus, "Profile persists whitelist and preserves identity/status");
        Status(await SendAsync(admin, HttpMethod.Put, path + "/profile", profile, original.RowVersion), HttpStatusCode.PreconditionFailed, "Profile stale version rejected");
        var other = await Details(otherPath);
        Status(await SendAsync(admin, HttpMethod.Put, otherPath + "/profile", profile, other.RowVersion), HttpStatusCode.Conflict, "Profile duplicate VIN needs reason");
        Status(await SendAsync(admin, HttpMethod.Put, otherPath + "/profile", new
            { modelId = model, vin, reason = "Correct second profile", duplicateVinReason = "Verified duplicate test" }, other.RowVersion), HttpStatusCode.NoContent, "Profile accepts authorized VIN exception");
        long reassignedModel;
        await using (var db = new SangRentalDbContext(options))
        {
            var currentModel = await db.Set<VehicleModel>().SingleAsync(x => x.Id == model);
            reassignedModel = await CreateAsync(admin, "/api/admin/catalog/models", new
                { categoryId = currentModel.VehicleCategoryId, brandId = currentModel.VehicleBrandId, code = "reassign-" + suffix, commercialName = "Reassigned model" });
        }
        Status(await SendAsync(admin, HttpMethod.Put, path + "/profile", new
            { modelId = reassignedModel, colorName = "Reassigned", reason = "Correct wrong model" }, updated.RowVersion), HttpStatusCode.NoContent, "Model reassignment allowed with reason");
        var reassigned = await Details(path);
        Check(reassigned.ModelId == reassignedModel && reassigned.Vin == null && reassigned.ManufacturingYear == null, "Profile clears optional values explicitly");
        await using (var db = new SangRentalDbContext(options))
        {
            Check(await db.Set<CatalogFleetAuditEvent>().AnyAsync(x => x.EntityType == "PhysicalVehicle" && x.EntityId == first
                && x.ActionCode == "ModelReassigned" && x.Reason == $"{model}->{reassignedModel}" && x.ActorReference == actor.ToString()), "Reassignment records old/new model and actor");
            Check(await db.Set<VehicleInspection>().CountAsync(x => x.PhysicalVehicleId == first) == 1, "Profile preserves inspection history");
        }

        var plate = "QA" + suffix.ToUpperInvariant();
        var plateBody = new { jurisdictionCode = "VN", plate, reason = "Assign test registration" };
        Status(await SendAsync(admin, HttpMethod.Post, path + "/registrations", plateBody), (HttpStatusCode)428, "Plate needs vehicle version");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/registrations", new { jurisdictionCode = "VN", plate = "...", reason = "Invalid" }, reassigned.RowVersion),
            HttpStatusCode.BadRequest, "Plate requires letters or digits");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/registrations", new { jurisdictionCode = "vn", plate, reason = "Invalid" }, reassigned.RowVersion),
            HttpStatusCode.BadRequest, "Jurisdiction code is validated");
        other = await Details(otherPath);
        var plateRace = await Task.WhenAll(
            SendAsync(admin, HttpMethod.Post, path + "/registrations", plateBody, reassigned.RowVersion),
            SendAsync(admin, HttpMethod.Post, otherPath + "/registrations", new
                { jurisdictionCode = "VN", plate = "qa-" + suffix, reason = "Same normalized registration" }, other.RowVersion));
        Check(plateRace.Count(x => x.StatusCode == HttpStatusCode.NoContent) == 1 && plateRace.Count(x => x.StatusCode == HttpStatusCode.Conflict) == 1,
            "Two vehicles racing same normalized plate have one winner");
        var ownerPath = plateRace[0].IsSuccessStatusCode ? path : otherPath;
        var loserPath = ownerPath == path ? otherPath : path;
        var owner = await Details(ownerPath);
        var loser = await Details(loserPath);
        Status(await SendAsync(admin, HttpMethod.Post, ownerPath + "/registrations", plateBody, owner.RowVersion),
            HttpStatusCode.Conflict, "Unchanged registration is rejected");
        Status(await SendAsync(admin, HttpMethod.Post, loserPath + "/registrations", new
            { jurisdictionCode = "US", plate, reason = "Different jurisdiction" }, loser.RowVersion), HttpStatusCode.NoContent, "Same plate in different jurisdiction allowed");
        var replacement = plate + "X";
        Status(await SendAsync(admin, HttpMethod.Post, ownerPath + "/registrations", new
            { jurisdictionCode = "VN", plate = replacement, reason = "Replace test plate" }, owner.RowVersion), HttpStatusCode.NoContent, "Plate replacement succeeds");
        Status(await SendAsync(admin, HttpMethod.Post, ownerPath + "/registrations", plateBody, owner.RowVersion),
            HttpStatusCode.PreconditionFailed, "Stale registration version rejected");
        var history = (await admin.GetFromJsonAsync<List<VehicleRegistrationSummary>>(ownerPath + "/registrations"))!;
        Check(history.Count == 2 && history.Count(x => x.ValidToUtc == null) == 1, "Exactly one current plate and old plate retained");
        var closed = history.Single(x => x.ValidToUtc != null);
        Check(closed.ClosureReason == "Replace test plate" && closed.ValidToUtc > closed.ValidFromUtc
            && closed.ValidToUtc == history.Single(x => x.ValidToUtc == null).ValidFromUtc, "Registration intervals meet without overlap");
        loser = await Details(loserPath);
        Status(await SendAsync(admin, HttpMethod.Post, loserPath + "/registrations", plateBody, loser.RowVersion),
            HttpStatusCode.NoContent, "Released old plate can be reassigned without deleting history");
        var beforeFailure = (await admin.GetFromJsonAsync<List<VehicleRegistrationSummary>>(loserPath + "/registrations"))!;
        loser = await Details(loserPath);
        Status(await SendAsync(admin, HttpMethod.Post, loserPath + "/registrations", new
            { jurisdictionCode = "VN", plate = replacement, reason = "Already used" }, loser.RowVersion), HttpStatusCode.Conflict, "Duplicate change rejected before closing existing registration");
        var afterFailure = (await admin.GetFromJsonAsync<List<VehicleRegistrationSummary>>(loserPath + "/registrations"))!;
        Check(beforeFailure.SequenceEqual(afterFailure) && (await Details(loserPath)).RowVersion == loser.RowVersion, "Rejected plate change preserves history and version");
        await using (var db = new SangRentalDbContext(options))
        {
            var historical = await db.Set<VehicleRegistration>().SingleAsync(x => x.Id == closed.Id);
            historical.ClosureReason = "Tampered history";
            await ExpectAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        await using (var db = new SangRentalDbContext(options))
        {
            var current = await db.Set<VehicleRegistration>().SingleAsync(x => x.PhysicalVehicleId == loser.Id && x.ValidToUtc == null);
            current.PlateDisplay = "Tampered plate";
            await ExpectAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        await using (var db = new SangRentalDbContext(options))
        {
            var registration = await db.Set<VehicleRegistration>().SingleAsync(x => x.Id == closed.Id);
            db.Remove(registration);
            await ExpectAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }

        var uiPath = $"/fleet/vehicles/{first}";
        var uiInput = await Details(path);
        var uiHtml = await blazor.GetStringAsync(uiPath);
        Check(uiHtml.Contains("fleet-profile") && uiHtml.Contains("fleet-registration"), "SSR renders profile and registration forms");
        var noReason = await SubmitSsrAsync(blazor, uiPath, "fleet-profile", new()
            { ["Input.ModelId"] = model.ToString(), ["Input.RowVersion"] = uiInput.RowVersion });
        Check(WebUtility.HtmlDecode(await noReason.Content.ReadAsStringAsync()).Contains("Vui lòng nhập lý do"), "SSR profile reason required inline");
        Status(await SubmitSsrAsync(blazor, uiPath, "fleet-profile", new()
            { ["Input.ModelId"] = model.ToString(), ["Input.ColorName"] = "UI blue", ["Input.ManufacturingYear"] = "2023",
                ["Input.Reason"] = "UI profile correction", ["Input.RowVersion"] = uiInput.RowVersion }), HttpStatusCode.Redirect, "SSR profile saves");
        var uiUpdated = await Details(path);
        Check(uiUpdated.ColorName == "UI blue" && uiUpdated.ManufacturingYear == 2023 && uiUpdated.ModelId == model, "SSR profile persisted");
        var badPlate = await SubmitSsrAsync(blazor, uiPath, "fleet-registration", new()
            { ["Input.JurisdictionCode"] = "VN", ["Input.Plate"] = "...", ["Input.Reason"] = "Invalid UI", ["Input.RowVersion"] = uiUpdated.RowVersion });
        Check(WebUtility.HtmlDecode(await badPlate.Content.ReadAsStringAsync()).Contains("Biển số cần chữ/số"), "SSR plate format invalid inline");
        var uiPlate = "UI" + suffix.ToUpperInvariant();
        Status(await SubmitSsrAsync(blazor, uiPath, "fleet-registration", new()
            { ["Input.JurisdictionCode"] = "VN", ["Input.Plate"] = uiPlate, ["Input.Reason"] = "UI change", ["Input.RowVersion"] = uiUpdated.RowVersion }),
            HttpStatusCode.Redirect, "SSR registration saves");
        Check((await blazor.GetStringAsync(uiPath)).Contains(uiPlate), "SSR history displays persisted plate");
        var uiStale = await SubmitSsrAsync(blazor, uiPath, "fleet-profile", new()
            { ["Input.ModelId"] = model.ToString(), ["Input.Reason"] = "Stale UI", ["Input.RowVersion"] = uiUpdated.RowVersion });
        Check(WebUtility.HtmlDecode(await uiStale.Content.ReadAsStringAsync()).Contains("Dữ liệu đã thay đổi"), "SSR profile and registration share concurrency token");
    }
}
