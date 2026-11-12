using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SangRental.Application.CatalogFleet;
using SangRental.Domain.CatalogFleet;
using SangRental.Infrastructure.Persistence;

internal static partial class IdentityIntegrationRunner
{
    private static async Task VerifyLifecycleAsync(HttpClient admin, HttpClient anonymous, HttpClient blazor,
        DbContextOptions<SangRentalDbContext> options, long model, string suffix, Guid actor)
    {
        var id = await CreateAsync(admin, "/api/admin/fleet/vehicles", new
            { modelId = model, assetCode = "LIFECYCLE-" + suffix, confirmInitialInspection = false });
        var path = $"/api/admin/fleet/vehicles/{id}";
        var input = new { reason = "Lifecycle integration inspection", confirmInspection = true };
        var draft = (await admin.GetFromJsonAsync<VehicleDetails>(path))!;
        Check(draft.RecordLifecycle == "Draft", "Lifecycle begins Draft");
        Status(await anonymous.GetAsync(path), HttpStatusCode.Unauthorized, "Vehicle details require authentication");
        Status(await admin.GetAsync("/api/admin/fleet/vehicles/9223372036854775807"), HttpStatusCode.NotFound, "Missing vehicle is 404");
        foreach (var action in new[] { "activate", "retire", "restore" })
            Status(await SendAsync(anonymous, HttpMethod.Post, path + "/" + action, input, draft.RowVersion),
                HttpStatusCode.Unauthorized, "Anonymous lifecycle denied: " + action);
        Status(await admin.PostAsJsonAsync(path + "/activate", input), HttpStatusCode.BadRequest, "Lifecycle requires CSRF");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/activate", input), (HttpStatusCode)428, "Lifecycle requires version");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/activate", input, "invalid"), HttpStatusCode.BadRequest, "Lifecycle rejects invalid version");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/activate", new { reason = "   ", confirmInspection = true }, draft.RowVersion),
            HttpStatusCode.BadRequest, "Lifecycle requires reason");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/activate", new { reason = "Unchecked inspection" }, draft.RowVersion),
            HttpStatusCode.Conflict, "Activation requires inspection confirmation");
        Check((await admin.GetFromJsonAsync<VehicleDetails>(path))!.RowVersion == draft.RowVersion, "Rejected activation leaves vehicle unchanged");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/restore", input, draft.RowVersion), HttpStatusCode.Conflict, "Draft cannot restore");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/retire", input, draft.RowVersion), HttpStatusCode.Conflict, "Draft cannot retire");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/activate", input, draft.RowVersion), HttpStatusCode.NoContent, "Draft activated");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/activate", input, draft.RowVersion), HttpStatusCode.PreconditionFailed, "Activation replay cannot write twice");
        var active = (await admin.GetFromJsonAsync<VehicleDetails>(path))!;
        Check(active.RecordLifecycle == "Active" && active.OperationalStatus == "Ready" && active.ReadyForRentAtUtc != null, "Activation persists readiness");
        var retireRace = await Task.WhenAll(
            SendAsync(admin, HttpMethod.Post, path + "/retire", input, active.RowVersion),
            SendAsync(admin, HttpMethod.Post, path + "/retire", input, active.RowVersion));
        Check(retireRace.Count(x => x.StatusCode == HttpStatusCode.NoContent) == 1
            && retireRace.Count(x => x.StatusCode == HttpStatusCode.PreconditionFailed) == 1, "Concurrent retirement commits once");
        var retired = (await admin.GetFromJsonAsync<VehicleDetails>(path))!;
        Check(retired.RecordLifecycle == "Retired" && retired.ReadyForRentAtUtc == null && retired.RetiredAtUtc != null
            && retired.RetirementReason == input.reason, "Retirement keeps reason and clears readiness timestamp");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/maintenance", new
            { scheduledStartUtc = DateTime.UtcNow, reason = "Retired schedule denied" }), HttpStatusCode.Conflict, "Retired vehicle cannot schedule maintenance");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/restore", new { reason = "Not inspected" }, retired.RowVersion),
            HttpStatusCode.Conflict, "Restoration requires inspection confirmation");
        Status(await SendAsync(admin, HttpMethod.Post, path + "/restore", input, retired.RowVersion), HttpStatusCode.NoContent, "Retired vehicle restored");
        var restored = (await admin.GetFromJsonAsync<VehicleDetails>(path))!;
        Check(restored.RecordLifecycle == "Active" && restored.OperationalStatus == "Ready"
            && restored.RetiredAtUtc == null && restored.RetirementReason == null, "Restoration resets current retirement fields");
        await using (var db = new SangRentalDbContext(options))
        {
            Check(await db.Set<VehicleInspection>().CountAsync(x => x.PhysicalVehicleId == id && x.ResultCode == "Passed") == 2,
                "Activation and restoration each have one Passed inspection");
            var events = await db.Set<CatalogFleetAuditEvent>().Where(x => x.EntityType == "PhysicalVehicle" && x.EntityId == id
                && (x.ActionCode == "Activated" || x.ActionCode == "Retired" || x.ActionCode == "Restored")).ToListAsync();
            Check(events.Count == 3 && events.All(x => x.ActorReference == actor.ToString() && x.Reason == input.reason),
                "Lifecycle audits retain authenticated actor and reasons exactly once");
        }

        // Lifecycle and maintenance must share a lock, not just a rowversion check.
        var race = await Task.WhenAll(
            SendAsync(admin, HttpMethod.Post, path + "/retire", input, restored.RowVersion),
            SendAsync(admin, HttpMethod.Post, path + "/maintenance", new
                { scheduledStartUtc = DateTime.UtcNow, reason = "Lifecycle vs maintenance race" }));
        Check(race.Count(x => x.IsSuccessStatusCode) == 1
            && race.Count(x => x.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed) == 1,
            "Retirement versus maintenance race has one winner");
        await using (var db = new SangRentalDbContext(options))
        {
            var unit = await db.Set<PhysicalVehicle>().SingleAsync(x => x.Id == id);
            Check(unit.RecordLifecycle != "Retired" || !await db.Set<MaintenanceEpisode>().AnyAsync(x => x.PhysicalVehicleId == id),
                "No retired vehicle with stranded maintenance after race");
        }

        // Actual SSR form posts: validation, stale version, persistence, no browser simulation.
        var uiCreate = await SubmitSsrAsync(blazor, "/fleet", "fleet-vehicle", new()
            { ["Input.ModelId"] = model.ToString(), ["Input.AssetCode"] = "UI-LIFE-" + suffix });
        Status(uiCreate, HttpStatusCode.Redirect, "Lifecycle SSR draft creation");
        var uiPath = uiCreate.Headers.Location!.ToString();
        var uiId = long.Parse(uiPath.Split('/').Last());
        var uiDraft = (await admin.GetFromJsonAsync<VehicleDetails>($"/api/admin/fleet/vehicles/{uiId}"))!;
        Check((await blazor.GetStringAsync(uiPath)).Contains("fleet-activate"), "Draft SSR offers activation");
        var uncheckedForm = await SubmitSsrAsync(blazor, uiPath, "fleet-activate", new()
            { ["Input.Reason"] = "UI test", ["Input.RowVersion"] = uiDraft.RowVersion });
        Check(WebUtility.HtmlDecode(await uncheckedForm.Content.ReadAsStringAsync()).Contains("Cần xác nhận kiểm tra xe"), "SSR activation requires inspection inline");
        var missingVersion = await SubmitSsrAsync(blazor, uiPath, "fleet-activate", new()
            { ["Input.Reason"] = "UI test", ["Input.ConfirmReview"] = "true" });
        Check(WebUtility.HtmlDecode(await missingVersion.Content.ReadAsStringAsync()).Contains("Thiếu phiên bản dữ liệu"), "SSR lifecycle requires posted version");
        Status(await SubmitSsrAsync(blazor, uiPath, "fleet-activate", new()
            { ["Input.Reason"] = "UI inspected", ["Input.ConfirmReview"] = "true", ["Input.RowVersion"] = uiDraft.RowVersion }),
            HttpStatusCode.Redirect, "SSR activation commits");
        var uiActive = (await admin.GetFromJsonAsync<VehicleDetails>($"/api/admin/fleet/vehicles/{uiId}"))!;
        var staleForm = await SubmitSsrAsync(blazor, uiPath, "fleet-retire", new()
            { ["Input.Reason"] = "Stale UI", ["Input.RowVersion"] = uiDraft.RowVersion });
        Check(WebUtility.HtmlDecode(await staleForm.Content.ReadAsStringAsync()).Contains("Dữ liệu đã thay đổi"), "SSR lifecycle preserves stale submitted version");
        Status(await SubmitSsrAsync(blazor, uiPath, "fleet-retire", new()
            { ["Input.Reason"] = "UI retirement", ["Input.RowVersion"] = uiActive.RowVersion }), HttpStatusCode.Redirect, "SSR retirement commits");
        var uiRetired = (await admin.GetFromJsonAsync<VehicleDetails>($"/api/admin/fleet/vehicles/{uiId}"))!;
        Check(uiRetired.RecordLifecycle == "Retired", "SSR retirement persisted");
        Check((await blazor.GetStringAsync(uiPath)).Contains("fleet-restore"), "Retired SSR offers restoration");
        Status(await SubmitSsrAsync(blazor, uiPath, "fleet-restore", new()
            { ["Input.Reason"] = "UI reinspected", ["Input.ConfirmReview"] = "true", ["Input.RowVersion"] = uiRetired.RowVersion }),
            HttpStatusCode.Redirect, "SSR restoration commits");
        Check((await admin.GetFromJsonAsync<VehicleDetails>($"/api/admin/fleet/vehicles/{uiId}"))!.RecordLifecycle == "Active", "SSR restoration persisted");
    }
}
