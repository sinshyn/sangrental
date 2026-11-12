extern alias ApiHost;
extern alias AdminHost;
using ApiEntryPoint = ApiHost::Program;
using AdminEntryPoint = AdminHost::Program;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SangRental.Application.CatalogFleet;
using SangRental.Domain.CatalogFleet;
using SangRental.Infrastructure.Persistence;
using SangRental.Infrastructure.Security;

internal static partial class IdentityIntegrationRunner
{
    private static int checks;
    private static string step = "startup";
    private static readonly DirectoryInfo TestKeys = new(Path.GetFullPath("artifacts/identity-integration-keys"));

    public static async Task<int> Main(string[] args)
    {
        if (args.Contains("--media-only"))
        {
            try { await VerifyMediaPrimitivesAsync(); Console.WriteLine($"PASS: {checks} media processing/storage checks (no SQL)."); return 0; }
            catch (Exception e) { Console.Error.WriteLine($"FAIL media primitives: {e.GetType().Name}: {e.Message}"); return 1; }
        }
        try { await RunAsync(); return 0; }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL at {step}: {(error is CheckFailedException ? error.Message : error.GetType().Name)}. No credentials or SQL payloads are printed.");
            return 1;
        }
    }

    private static async Task RunAsync()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets("SangRental-Api-development")
            .AddEnvironmentVariables().Build();
        var connection = new SqlConnectionStringBuilder(configuration.GetConnectionString("SangRental")
            ?? throw new InvalidOperationException("Configure the local SQL connection first."));
        if (connection.DataSource.Equals("localhost,1436", StringComparison.OrdinalIgnoreCase))
            connection.DataSource = "tcp:127.0.0.1,1436";
        if (connection.DataSource is not ("tcp:127.0.0.1,1436" or "127.0.0.1,1436"))
            throw new InvalidOperationException("Integration tests allow only the approved local SQL endpoint.");
        connection.InitialCatalog = "SangRentalTestDb";
        var options = new DbContextOptionsBuilder<SangRentalDbContext>().UseSqlServer(connection.ConnectionString).Options;
        step = "migrate SangRentalTestDb";
        await using (var db = new SangRentalDbContext(options))
        {
            // Explicitly authorized test target only; never delete/drop a database or use EnsureCreated.
            await db.Database.MigrateAsync();
            Check((await db.Database.GetAppliedMigrationsAsync()).Count() == 3, "Three migrations applied to test DB");
        }
        await using var factory = CreateFactory(connection.ConnectionString);
        var password = "Sang!9a" + Guid.NewGuid().ToString("N");
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var adminName = "admin-" + suffix;
        var staffName = "staff-" + suffix;
        var managerName = "manager-" + suffix;
        var createdUsers = new List<Guid>();
        try
        {
            step = "provision isolated identities";
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                foreach (var name in new[] { "Admin", "Manager", "Staff" })
                    if (!await roles.RoleExistsAsync(name)) Success(await roles.CreateAsync(new IdentityRole<Guid>(name) { Id = Guid.NewGuid() }));
                var bootstrap = scope.ServiceProvider.GetRequiredService<AdminProvisioningService>();
                if ((await users.GetUsersInRoleAsync("Admin")).Count == 0)
                {
                    await bootstrap.ProvisionFirstAdminAsync(adminName, password);
                    createdUsers.Add((await users.FindByNameAsync(adminName))!.Id);
                    Check(true, "First Admin bootstrap");
                }
                else createdUsers.Add(await CreateUserAsync(users, adminName, password, "Admin"));
                await ExpectAsync<InvalidOperationException>(() => bootstrap.ProvisionFirstAdminAsync("second-" + suffix, password));
                await bootstrap.ProvisionLocalTestAdminAsync("manual-" + suffix, password);
                createdUsers.Add((await users.FindByNameAsync("manual-" + suffix))!.Id);
                Check(true, "Additional local test administrator provisioning");
                createdUsers.Add(await CreateUserAsync(users, staffName, password, "Staff"));
                createdUsers.Add(await CreateUserAsync(users, managerName, password, "Manager"));
            }
            using var anonymous = Client(factory);
            step = "anonymous and CSRF boundaries";
            Status(await anonymous.GetAsync("/auth/session"), HttpStatusCode.Unauthorized, "Anonymous session denied");
            Status(await anonymous.GetAsync("/api/admin/fleet/vehicles"), HttpStatusCode.Unauthorized, "Anonymous fleet denied");
            Status(await anonymous.PostAsJsonAsync("/auth/login", new { username = adminName, password }),
                HttpStatusCode.BadRequest, "Login without CSRF denied");
            using var httpClient = factory.CreateClient(new WebApplicationFactoryClientOptions
                { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false });
            Status(await httpClient.PostAsJsonAsync("/auth/login", new { username = adminName, password }),
                HttpStatusCode.BadRequest, "HTTP login denied");
            Status(await anonymous.PostAsJsonAsync("/auth/register", new { username = adminName, password }),
                HttpStatusCode.NotFound, "No public registration");
            var schema = await anonymous.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
            Check(schema.GetProperty("paths").TryGetProperty("/api/admin/fleet/vehicles", out _), "Fleet appears in OpenAPI");
            Status(await anonymous.GetAsync("/swagger/index.html"), HttpStatusCode.OK, "Swagger UI served");

            step = "administrator login and catalog creation";
            using var admin = Client(factory);
            var login = await LoginAsync(admin, adminName, password);
            Status(login, HttpStatusCode.NoContent, "Administrator login");
            var cookie = login.Headers.GetValues("Set-Cookie").Single(v => v.StartsWith("__Host-SangRental.Admin="));
            Check(cookie.Contains("secure", StringComparison.OrdinalIgnoreCase)
                && cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase)
                && cookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase), "Secure HttpOnly Strict cookie");
            var copiedCookie = cookie.Split(';')[0];
            Status(await admin.GetAsync("/auth/session"), HttpStatusCode.OK, "Authenticated session");

            step = "Blazor form and shared cookie";
            await using var adminFactory = new WebApplicationFactory<AdminEntryPoint>().WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(Path.GetFullPath("admin/SangRental.Admin"));
                builder.UseEnvironment("Development");
                ConfigureTestServices(builder, connection.ConnectionString);
            });
            using var blazor = adminFactory.CreateClient(new WebApplicationFactoryClientOptions
                { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            Status(await blazor.GetAsync("/"), HttpStatusCode.Redirect, "Blazor dashboard requires login");
            Status(await blazor.GetAsync("/catalog"), HttpStatusCode.Redirect, "Catalog SSR requires login");
            var loginHtml = await blazor.GetStringAsync("/account/login");
            Check(loginHtml.Contains("autocomplete=\"current-password\""), "SSR login form rendered");
            var formToken = Regex.Match(loginHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Check(formToken.Success, "Login form has antiforgery token");
            Status(await blazor.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
                { ["username"] = adminName, ["password"] = password })), HttpStatusCode.BadRequest, "Blazor CSRF enforced");
            var formLogin = await blazor.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = adminName, ["password"] = password,
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(formToken.Groups[1].Value)
            }));
            Status(formLogin, HttpStatusCode.Redirect, "Blazor form login");
            Status(await blazor.GetAsync("/auth/session"), HttpStatusCode.NoContent, "Blazor session authorized");
            step = "catalog SSR forms";
            var catalogHtml = await blazor.GetStringAsync("/catalog");
            Check(catalogHtml.Contains("create-category") && catalogHtml.Contains("create-brand") && catalogHtml.Contains("create-model"), "Catalog has three SSR forms");
            Status(await blazor.PostAsync("/catalog", new FormUrlEncodedContent(new Dictionary<string, string>
                { ["_handler"] = "create-brand", ["Draft.Code"] = "csrf-denied-" + suffix, ["Draft.Name"] = "Denied" })),
                HttpStatusCode.BadRequest, "Catalog SSR mutation requires CSRF");
            var catalogToken = Regex.Match(catalogHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Check(catalogToken.Success, "Catalog SSR has CSRF token");
            async Task<HttpResponseMessage> SubmitCatalogAsync(string handler, string code, string name, string? categoryId = null, string? brandId = null, string? family = null)
            {
                var values = new Dictionary<string, string> { ["_handler"] = handler, ["Draft.Code"] = code, ["Draft.Name"] = name,
                    ["__RequestVerificationToken"] = WebUtility.HtmlDecode(catalogToken.Groups[1].Value) };
                if (categoryId is not null) values["Draft.CategoryId"] = categoryId;
                if (brandId is not null) values["Draft.BrandId"] = brandId;
                if (family is not null) values["Draft.Family"] = family;
                return await blazor.PostAsync("/catalog", new FormUrlEncodedContent(values));
            }
            var invalidForm = await SubmitCatalogAsync("create-brand", "UPPER CASE", "Invalid");
            Status(invalidForm, HttpStatusCode.OK, "Catalog validation re-renders form");
            Check(WebUtility.HtmlDecode(await invalidForm.Content.ReadAsStringAsync()).Contains("Mã không đúng định dạng."), "Catalog validation is inline");
            var ssrCode = "ssr-" + suffix;
            var ssrCreate = await SubmitCatalogAsync("create-brand", ssrCode, "SSR Brand");
            Status(ssrCreate, HttpStatusCode.Redirect, "Catalog SSR brand creation");
            Status(await SubmitCatalogAsync("create-brand", ssrCode, "Duplicate"), HttpStatusCode.OK, "Catalog SSR duplicate re-renders inline");
            await using (var ssrDb = new SangRentalDbContext(options))
            {
                var createdBrand = await ssrDb.Set<VehicleBrand>().SingleAsync(x => x.Code == ssrCode);
                Check(await ssrDb.Set<CatalogFleetAuditEvent>().AnyAsync(x => x.EntityType == "VehicleBrand" && x.EntityId == createdBrand.Id
                    && x.ActorReference == createdUsers[0].ToString()), "SSR mutation has authenticated audit actor");
                Status(await SubmitCatalogAsync("create-category", ssrCode, "SSR Category", family: "Motorcycle"), HttpStatusCode.Redirect, "Catalog SSR category creation");
                var ssrCategory = await ssrDb.Set<VehicleCategory>().SingleAsync(x => x.Code == ssrCode);
                Status(await SubmitCatalogAsync("create-model", ssrCode, "SSR Model", ssrCategory.Id.ToString(), createdBrand.Id.ToString()), HttpStatusCode.Redirect, "Catalog SSR model creation");
                Check(await ssrDb.Set<VehicleModel>().AnyAsync(x => x.Code == ssrCode && x.PublicationStatus == "Draft"), "SSR model remains unpublished");
                Status(await SubmitCatalogAsync("create-model", "missing-" + suffix, "Missing parent", long.MaxValue.ToString(), createdBrand.Id.ToString()), HttpStatusCode.OK, "Missing parent re-renders form");
                Check(!await ssrDb.Set<VehicleModel>().AnyAsync(x => x.Code == "missing-" + suffix), "Invalid parent does not create model");
            }
            var createdPage = await blazor.GetStringAsync("/catalog");
            Check(createdPage.Contains(ssrCode), "Catalog displays persisted SSR records");
            using var sharedApiClient = Client(factory);
            sharedApiClient.DefaultRequestHeaders.Add("Cookie", formLogin.Headers.GetValues("Set-Cookie")
                .Single(v => v.StartsWith("__Host-SangRental.Admin=")).Split(';')[0]);
            Status(await sharedApiClient.GetAsync("/api/admin/fleet/vehicles"), HttpStatusCode.OK, "Blazor cookie accepted by API");
            using var sharedAdminClient = adminFactory.CreateClient(new WebApplicationFactoryClientOptions
                { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            sharedAdminClient.DefaultRequestHeaders.Add("Cookie", copiedCookie);
            Status(await sharedAdminClient.GetAsync("/auth/session"), HttpStatusCode.NoContent, "API cookie accepted by Blazor");
            step = "administrator login and catalog creation";
            var category = await CreateAsync(admin, "/api/admin/catalog/categories", new { code = "test-" + suffix, vehicleFamilyCode = "Motorcycle", nameVi = "Loại kiểm thử" });
            var brand = await CreateAsync(admin, "/api/admin/catalog/brands", new { code = "test-" + suffix, displayName = "Integration Test" });
            var model = await CreateAsync(admin, "/api/admin/catalog/models", new { categoryId = category, brandId = brand, code = "test-" + suffix, commercialName = "Integration Test" });
            step = "fleet lifecycle API and SSR";
            await VerifyLifecycleAsync(admin, anonymous, blazor, options, model, suffix, createdUsers[0]);
            step = "vehicle profile and registration history";
            await VerifyProfileRegistrationAsync(admin, blazor, options, model, suffix, createdUsers[0]);
            step = "catalog media upload/cache/SSR";
            await VerifyCatalogMediaAsync(admin, anonymous, blazor, factory.Services, options, model, suffix);
            step = "fleet SSR workflows";
            using var fleetAnonymous = adminFactory.CreateClient(new WebApplicationFactoryClientOptions
                { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            Status(await fleetAnonymous.GetAsync("/fleet"), HttpStatusCode.Redirect, "Fleet SSR requires login");
            Status(await blazor.PostAsync("/fleet", new FormUrlEncodedContent(new Dictionary<string, string>
                { ["_handler"] = "fleet-vehicle", ["Input.AssetCode"] = "DENIED-" + suffix })), HttpStatusCode.BadRequest, "Fleet SSR requires CSRF");
            var invalidVehicle = await SubmitSsrAsync(blazor, "/fleet", "fleet-vehicle", new() { ["Input.AssetCode"] = "Invalid" });
            Status(invalidVehicle, HttpStatusCode.OK, "Fleet invalid input re-renders");
            Check(WebUtility.HtmlDecode(await invalidVehicle.Content.ReadAsStringAsync()).Contains("ID mẫu xe phải lớn hơn 0."), "Fleet model validation inline");
            var draftCreate = await SubmitSsrAsync(blazor, "/fleet", "fleet-vehicle", new()
                { ["Input.ModelId"] = model.ToString(), ["Input.AssetCode"] = "SSR-DRAFT-" + suffix });
            Status(draftCreate, HttpStatusCode.Redirect, "SSR draft vehicle creation");
            var readyCreate = await SubmitSsrAsync(blazor, "/fleet", "fleet-vehicle", new()
                { ["Input.ModelId"] = model.ToString(), ["Input.AssetCode"] = "SSR-READY-" + suffix, ["Input.ConfirmInitialInspection"] = "true" });
            Status(readyCreate, HttpStatusCode.Redirect, "SSR inspected Ready vehicle creation");
            var readyPath = readyCreate.Headers.Location!.ToString();
            var readyId = long.Parse(readyPath.Split('/').Last());
            Check(!(await blazor.GetStringAsync(draftCreate.Headers.Location!.ToString())).Contains("fleet-schedule"), "Draft vehicle has no schedule form");
            var invalidInterval = await SubmitSsrAsync(blazor, readyPath, "fleet-schedule", new()
                { ["Input.StartLocal"] = "2026-11-11T10:00", ["Input.EndLocal"] = "2026-11-11T09:00", ["Input.Reason"] = "Invalid interval" });
            Check(WebUtility.HtmlDecode(await invalidInterval.Content.ReadAsStringAsync()).Contains("Giờ kết thúc phải sau giờ bắt đầu."), "SSR reversed interval rejected inline");
            await using (var uiDb = new SangRentalDbContext(options))
            {
                Check(await uiDb.Set<PhysicalVehicle>().AnyAsync(x => x.AssetCode == "SSR-DRAFT-" + suffix && x.RecordLifecycle == "Draft" && x.OperationalStatus == "PendingInspection"), "SSR unchecked readiness stays draft");
                Check(await uiDb.Set<VehicleInspection>().AnyAsync(x => x.PhysicalVehicleId == readyId && x.InspectionTypeCode == "InitialReadiness"), "SSR Ready has inspection evidence");
            }
            var duplicateVehicle = await SubmitSsrAsync(blazor, "/fleet", "fleet-vehicle", new()
                { ["Input.ModelId"] = model.ToString(), ["Input.AssetCode"] = "SSR-READY-" + suffix });
            Check(WebUtility.HtmlDecode(await duplicateVehicle.Content.ReadAsStringAsync()).Contains("Mã tài sản đã tồn tại."), "Duplicate asset is inline");
            var vietnamNow = DateTime.UtcNow.AddHours(7).AddMinutes(-1).ToString("yyyy-MM-ddTHH:mm");
            var schedule = await SubmitSsrAsync(blazor, readyPath, "fleet-schedule", new()
                { ["Input.StartLocal"] = vietnamNow, ["Input.Reason"] = "SSR maintenance" });
            Status(schedule, HttpStatusCode.Redirect, "SSR maintenance scheduled");
            var maintenancePath = schedule.Headers.Location!.ToString();
            var uiEpisodeId = long.Parse(maintenancePath.Split('/').Last());
            var overlapForm = await SubmitSsrAsync(blazor, readyPath, "fleet-schedule", new()
                { ["Input.StartLocal"] = vietnamNow, ["Input.Reason"] = "SSR overlap" });
            Check(WebUtility.HtmlDecode(await overlapForm.Content.ReadAsStringAsync()).Contains("Lịch bảo trì trùng"), "SSR maintenance overlap blocked inline");
            await using (var uiDb = new SangRentalDbContext(options))
            {
                var uiEpisode = await uiDb.Set<MaintenanceEpisode>().AsNoTracking().SingleAsync(x => x.Id == uiEpisodeId);
                Check(Math.Abs((uiEpisode.ScheduledStartUtc - DateTime.UtcNow).TotalMinutes) < 3, "Vietnam local input converts to UTC");
                var earlyComplete = await SubmitSsrAsync(blazor, maintenancePath, "fleet-complete", new()
                    { ["Input.RowVersion"] = Convert.ToBase64String(uiEpisode.RowVersion), ["Input.ConfirmReview"] = "true" });
                Check(WebUtility.HtmlDecode(await earlyComplete.Content.ReadAsStringAsync()).Contains("Checklist chưa đầy đủ"), "SSR release requires complete checklist");
                Status(await SubmitSsrAsync(blazor, maintenancePath, "fleet-add-task", new() { ["Input.Title"] = "SSR inspection" }), HttpStatusCode.Redirect, "SSR checklist item added");
                var uiTask = await uiDb.Set<MaintenanceChecklistItem>().AsNoTracking().SingleAsync(x => x.MaintenanceEpisodeId == uiEpisodeId);
                var version = Convert.ToBase64String(uiTask.RowVersion);
                var malformedVersion = await SubmitSsrAsync(blazor, maintenancePath, $"fleet-task-{uiTask.Id}", new()
                    { ["Input.Status"] = "Completed", ["Input.RowVersion"] = "not-base64" });
                Check(WebUtility.HtmlDecode(await malformedVersion.Content.ReadAsStringAsync()).Contains("Thiếu phiên bản dữ liệu"), "SSR malformed version rejected inline");
                var noVersion = await SubmitSsrAsync(blazor, maintenancePath, $"fleet-task-{uiTask.Id}", new() { ["Input.Status"] = "Completed" });
                Check(WebUtility.HtmlDecode(await noVersion.Content.ReadAsStringAsync()).Contains("Thiếu phiên bản dữ liệu"), "SSR checklist needs version");
                var skipWithoutReason = await SubmitSsrAsync(blazor, maintenancePath, $"fleet-task-{uiTask.Id}", new()
                    { ["Input.Status"] = "Skipped", ["Input.RowVersion"] = version });
                Check(WebUtility.HtmlDecode(await skipWithoutReason.Content.ReadAsStringAsync()).Contains("Vui lòng nhập lý do bỏ qua."), "SSR skipped item needs reason");
                Status(await SubmitSsrAsync(blazor, maintenancePath, $"fleet-task-{uiTask.Id}", new()
                    { ["Input.Status"] = "Completed", ["Input.Note"] = "Passed", ["Input.RowVersion"] = version }), HttpStatusCode.Redirect, "SSR task completed");
                var stale = await SubmitSsrAsync(blazor, maintenancePath, $"fleet-task-{uiTask.Id}", new()
                    { ["Input.Status"] = "Skipped", ["Input.SkipReason"] = "Stale request", ["Input.RowVersion"] = version });
                Check(WebUtility.HtmlDecode(await stale.Content.ReadAsStringAsync()).Contains("Dữ liệu đã thay đổi."), "SSR stale task version rejected");
                var confirmMissing = await SubmitSsrAsync(blazor, maintenancePath, "fleet-complete", new() { ["Input.RowVersion"] = Convert.ToBase64String(uiEpisode.RowVersion) });
                Check(WebUtility.HtmlDecode(await confirmMissing.Content.ReadAsStringAsync()).Contains("Cần xác nhận kiểm tra xe"), "SSR release requires explicit inspection confirmation");
                Status(await SubmitSsrAsync(blazor, maintenancePath, "fleet-complete", new()
                    { ["Input.RowVersion"] = Convert.ToBase64String(uiEpisode.RowVersion), ["Input.ConfirmReview"] = "true" }), HttpStatusCode.Redirect, "SSR maintenance release");
                Check(await uiDb.Set<PhysicalVehicle>().AnyAsync(x => x.Id == readyId && x.OperationalStatus == "Ready"), "SSR release persisted Ready");
                Check(await uiDb.Set<MaintenanceEpisode>().AnyAsync(x => x.Id == uiEpisodeId && x.StatusCode == "Completed" && x.ReleaseInspectionId != null), "SSR release persisted inspection and end state");
            }
            Check(!(await blazor.GetStringAsync(maintenancePath)).Contains("fleet-complete"), "Completed maintenance has no release form");
            Status(await SendAsync(admin, HttpMethod.Post, "/api/admin/catalog/brands", new { code = "test-" + suffix, displayName = "Duplicate" }),
                HttpStatusCode.Conflict, "Duplicate business key rejected");
            Status(await SendAsync(admin, HttpMethod.Post, "/api/admin/catalog/brands", new { code = "UPPER CASE", displayName = "Invalid" }),
                HttpStatusCode.BadRequest, "Invalid canonical code rejected");
            var vehicle = await CreateAsync(admin, "/api/admin/fleet/vehicles", new { modelId = model, assetCode = "TEST-" + suffix, confirmInitialInspection = true });
            await using (var db = new SangRentalDbContext(options))
            {
                Check(await db.Set<VehicleInspection>().AnyAsync(i => i.PhysicalVehicleId == vehicle && i.ResultCode == "Passed"), "Ready inspection persisted");
                var actor = await db.Set<CatalogFleetAuditEvent>().Where(a => a.EntityType == "PhysicalVehicle" && a.EntityId == vehicle)
                    .Select(a => a.ActorReference).FirstAsync();
                Check(actor == createdUsers[0].ToString(), "Trusted actor persisted");
            }
            step = "maintenance HTTP concurrency";
            var start = DateTime.UtcNow.AddMinutes(-1);
            var episode = await CreateAsync(admin, $"/api/admin/fleet/vehicles/{vehicle}/maintenance", new { scheduledStartUtc = start, reason = "Integration Test" });
            Status(await SendAsync(admin, HttpMethod.Post, $"/api/admin/fleet/vehicles/{vehicle}/maintenance", new { scheduledStartUtc = start, reason = "Overlap" }),
                HttpStatusCode.Conflict, "Maintenance overlap rejected");
            var task = await CreateAsync(admin, $"/api/admin/fleet/maintenance/{episode}/tasks", new { customTitleVi = "Kiểm tra thử" });
            var taskRows = await admin.GetFromJsonAsync<List<MaintenanceTaskSummary>>($"/api/admin/fleet/maintenance/{episode}/tasks");
            var oldVersion = taskRows!.Single(x => x.Id == task).RowVersion;
            Status(await SendAsync(admin, HttpMethod.Put, $"/api/admin/fleet/tasks/{task}", new { status = "Completed" }),
                HttpStatusCode.PreconditionRequired, "If-Match required");
            Status(await SendAsync(admin, HttpMethod.Put, $"/api/admin/fleet/tasks/{task}", new { status = "Completed" }, oldVersion),
                HttpStatusCode.NoContent, "Checklist update with correct version");
            Status(await SendAsync(admin, HttpMethod.Put, $"/api/admin/fleet/tasks/{task}", new { status = "Skipped" }, oldVersion),
                HttpStatusCode.PreconditionFailed, "Stale version denied");
            var episodes = await admin.GetFromJsonAsync<List<MaintenanceSummary>>($"/api/admin/fleet/vehicles/{vehicle}/maintenance");
            var episodeVersion = episodes!.Single(x => x.Id == episode).RowVersion;
            Status(await SendAsync(admin, HttpMethod.Post, $"/api/admin/fleet/maintenance/{episode}/complete", null, episodeVersion),
                HttpStatusCode.NoContent, "Maintenance completed");
            Status(await SendAsync(admin, HttpMethod.Post, $"/api/admin/fleet/maintenance/{episode}/complete", null, episodeVersion),
                HttpStatusCode.Conflict, "Duplicate completion rejected");

            step = "concurrent maintenance requests";
            var raceVehicle = await CreateAsync(admin, "/api/admin/fleet/vehicles", new
                { modelId = model, assetCode = "RACE-" + suffix, confirmInitialInspection = true });
            var racePath = $"/api/admin/fleet/vehicles/{raceVehicle}/maintenance";
            var concurrent = await Task.WhenAll(
                SendAsync(admin, HttpMethod.Post, racePath, new { scheduledStartUtc = start, reason = "Race A" }),
                SendAsync(admin, HttpMethod.Post, racePath, new { scheduledStartUtc = start, reason = "Race B" }));
            Check(concurrent.Count(r => r.StatusCode == HttpStatusCode.Created) == 1
                && concurrent.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1, "Only one concurrent maintenance create succeeds");
            var raceEpisode = (await concurrent.Single(r => r.StatusCode == HttpStatusCode.Created)
                .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
            var raceTask = await CreateAsync(admin, $"/api/admin/fleet/maintenance/{raceEpisode}/tasks", new { customTitleVi = "Race task" });
            var raceTasks = await admin.GetFromJsonAsync<List<MaintenanceTaskSummary>>($"/api/admin/fleet/maintenance/{raceEpisode}/tasks");
            var raceVersion = raceTasks!.Single(x => x.Id == raceTask).RowVersion;
            var taskRace = await Task.WhenAll(
                SendAsync(admin, HttpMethod.Put, $"/api/admin/fleet/tasks/{raceTask}", new { status = "Completed" }, raceVersion),
                SendAsync(admin, HttpMethod.Put, $"/api/admin/fleet/tasks/{raceTask}", new { status = "Completed" }, raceVersion));
            Check(taskRace.Count(r => r.StatusCode == HttpStatusCode.NoContent) == 1
                && taskRace.Count(r => r.StatusCode == HttpStatusCode.PreconditionFailed) == 1, "Only one concurrent checklist update succeeds");
            var raceEpisodes = await admin.GetFromJsonAsync<List<MaintenanceSummary>>(racePath);
            var raceEpisodeVersion = raceEpisodes!.Single(x => x.Id == raceEpisode).RowVersion;
            var completeRace = await Task.WhenAll(
                SendAsync(admin, HttpMethod.Post, $"/api/admin/fleet/maintenance/{raceEpisode}/complete", null, raceEpisodeVersion),
                SendAsync(admin, HttpMethod.Post, $"/api/admin/fleet/maintenance/{raceEpisode}/complete", null, raceEpisodeVersion));
            Check(completeRace.Count(r => r.StatusCode == HttpStatusCode.NoContent) == 1
                && completeRace.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1, "Only one concurrent maintenance release succeeds");
            var overdueVehicle = await CreateAsync(admin, "/api/admin/fleet/vehicles", new
                { modelId = model, assetCode = "OVERDUE-" + suffix, confirmInitialInspection = true });
            var overduePath = $"/api/admin/fleet/vehicles/{overdueVehicle}/maintenance";
            await CreateAsync(admin, overduePath, new { scheduledStartUtc = DateTime.UtcNow.AddHours(-3),
                scheduledEndUtc = DateTime.UtcNow.AddHours(-2), reason = "Unreleased overdue maintenance" });
            Status(await SendAsync(admin, HttpMethod.Post, overduePath, new { scheduledStartUtc = DateTime.UtcNow, reason = "Must remain blocked" }),
                HttpStatusCode.Conflict, "Overdue unreleased maintenance still blocks new interval");
            await using (var db = new SangRentalDbContext(options))
            {
                var history = await db.Set<VehicleInspection>().FirstAsync(i => i.PhysicalVehicleId == vehicle);
                history.Findings = "Tampered";
                await ExpectAsync<InvalidOperationException>(() => db.SaveChangesAsync());
            }

            step = "role and permission revocation";
            using var staff = Client(factory);
            Status(await LoginAsync(staff, staffName, password), HttpStatusCode.NoContent, "Staff can authenticate");
            Status(await SendAsync(staff, HttpMethod.Post, $"/api/admin/catalog/models/{model}/images", null), HttpStatusCode.Forbidden, "Staff cannot upload catalog images");
            Status(await staff.GetAsync($"/api/admin/catalog/models/{model}/images"), HttpStatusCode.Forbidden, "Staff cannot list catalog images");
            foreach (var action in new[] { "activate", "retire", "restore" })
                Status(await SendAsync(staff, HttpMethod.Post, $"/api/admin/fleet/vehicles/{vehicle}/{action}",
                    new { reason = "Denied", confirmInspection = true }, Convert.ToBase64String(new byte[8])),
                    HttpStatusCode.Forbidden, "Staff cannot " + action);
            Status(await SendAsync(staff, HttpMethod.Put, $"/api/admin/fleet/vehicles/{vehicle}/profile",
                new { modelId = model, reason = "Denied" }, Convert.ToBase64String(new byte[8])), HttpStatusCode.Forbidden, "Staff cannot edit profile");
            Status(await SendAsync(staff, HttpMethod.Post, $"/api/admin/fleet/vehicles/{vehicle}/registrations",
                new { jurisdictionCode = "VN", plate = "DENIED", reason = "Denied" }, Convert.ToBase64String(new byte[8])), HttpStatusCode.Forbidden, "Staff cannot change plate");
            Status(await staff.GetAsync("/api/admin/fleet/vehicles"), HttpStatusCode.Forbidden, "Staff cannot read fleet");
            using var staffBlazor = adminFactory.CreateClient(new WebApplicationFactoryClientOptions
                { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            using var staffCookieClient = Client(factory);
            var staffLogin = await LoginAsync(staffCookieClient, staffName, password);
            staffBlazor.DefaultRequestHeaders.Add("Cookie", staffLogin.Headers.GetValues("Set-Cookie")
                .Single(v => v.StartsWith("__Host-SangRental.Admin=")).Split(';')[0]);
            Status(await staffBlazor.GetAsync("/catalog"), HttpStatusCode.Redirect, "Staff cannot access Catalog SSR");
            Status(await staffBlazor.GetAsync("/fleet"), HttpStatusCode.Redirect, "Staff cannot access Fleet SSR");
            Status(await staffBlazor.GetAsync(maintenancePath), HttpStatusCode.Redirect, "Staff cannot access maintenance SSR");
            Status(await SendAsync(staff, HttpMethod.Post, "/api/admin/catalog/brands", new { code = "denied-" + suffix, displayName = "Denied" }),
                HttpStatusCode.Forbidden, "Staff cannot mutate catalog");
            using var manager = Client(factory);
            Status(await LoginAsync(manager, managerName, password), HttpStatusCode.NoContent, "Manager can authenticate");
            Status(await manager.GetAsync("/api/admin/fleet/vehicles"), HttpStatusCode.Forbidden, "Manager requires explicit permission");
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
                var user = (await users.FindByNameAsync(managerName))!;
                Success(await users.AddClaimAsync(user, new Claim(AdminIdentityServices.PermissionClaim, AdminIdentityServices.ManageFleet)));
            }
            Status(await manager.GetAsync("/api/admin/fleet/vehicles"), HttpStatusCode.OK, "Permission changes read from database");
            Status(await SendAsync(manager, HttpMethod.Post, $"/api/admin/catalog/models/{model}/images", null), HttpStatusCode.Forbidden, "Fleet permission cannot upload catalog images");
            foreach (var action in new[] { "activate", "retire", "restore", "registrations" })
                Status(await SendAsync(manager, HttpMethod.Post, $"/api/admin/fleet/vehicles/{vehicle}/{action}",
                    new { reason = "Manager denied", confirmInspection = true, jurisdictionCode = "VN", plate = "DENIED" }, Convert.ToBase64String(new byte[8])),
                    HttpStatusCode.Forbidden, "Fleet permission does not grant Admin action " + action);
            Status(await SendAsync(manager, HttpMethod.Put, $"/api/admin/fleet/vehicles/{vehicle}/profile",
                new { modelId = model, reason = "Manager denied" }, Convert.ToBase64String(new byte[8])), HttpStatusCode.Forbidden, "Manager cannot edit profile");
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
                var user = (await users.FindByNameAsync(managerName))!;
                Success(await users.RemoveClaimAsync(user, new Claim(AdminIdentityServices.PermissionClaim, AdminIdentityServices.ManageFleet)));
            }
            Status(await manager.GetAsync("/api/admin/fleet/vehicles"), HttpStatusCode.Forbidden, "Revoked permission effective immediately");
            using var copied = Client(factory);
            copied.DefaultRequestHeaders.Add("Cookie", copiedCookie);
            Status(await copied.GetAsync("/auth/session"), HttpStatusCode.OK, "Copied cookie initially valid");
            Status(await SendAsync(admin, HttpMethod.Post, "/auth/logout", null), HttpStatusCode.NoContent, "Logout revokes sessions");
            Status(await copied.GetAsync("/auth/session"), HttpStatusCode.Unauthorized, "Copied cookie rejected after logout");
            Status(await blazor.GetAsync("/auth/session"), HttpStatusCode.Unauthorized, "API logout also revokes Blazor session");
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
                var user = (await users.FindByNameAsync(staffName))!;
                user.IsActive = false;
                Success(await users.UpdateAsync(user));
            }
            Status(await staff.GetAsync("/auth/session"), HttpStatusCode.Unauthorized, "Disabled user loses session");

            step = "lockout and rate limit";
            await using var lockoutFactory = CreateFactory(connection.ConnectionString);
            using var lockedClient = Client(lockoutFactory);
            for (var i = 0; i < 5; i++)
                Status(await LoginAsync(lockedClient, managerName, "Wrong!9" + Guid.NewGuid()), HttpStatusCode.Unauthorized, "Wrong password rejected");
            Status(await LoginAsync(lockedClient, managerName, password), HttpStatusCode.Unauthorized, "Locked account cannot sign in");
            for (var i = 0; i < 4; i++) await LoginAsync(lockedClient, "", "");
            Status(await LoginAsync(lockedClient, "", ""), HttpStatusCode.TooManyRequests, "Login endpoint rate limited");
            step = "verify database boundaries";
            var development = new SqlConnectionStringBuilder(connection.ConnectionString) { InitialCatalog = "SangRentalDb" };
            await using (var developmentConnection = new SqlConnection(development.ConnectionString))
            {
                await developmentConnection.OpenAsync();
                await using var command = developmentConnection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE SCHEMA_NAME(schema_id) = 'sangrental'; " +
                    "SELECT COUNT(*) FROM sys.tables WHERE SCHEMA_NAME(schema_id) = 'identity'; " +
                    "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory;";
                await using var reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();
                Check(reader.GetInt32(0) == 16, "Development still has 16 catalog tables");
                await reader.NextResultAsync(); await reader.ReadAsync();
                Check(reader.GetInt32(0) == 0, "Identity migration was not applied to Development");
                await reader.NextResultAsync(); await reader.ReadAsync();
                Check(reader.GetInt32(0) == 2, "Development still has only the two approved migrations");
            }
            Console.WriteLine($"PASS: {checks} Identity/API SQL integration checks on SangRentalTestDb only.");
        }
        finally
        {
            // Keep test records/audit for inspection; disable generated accounts and revoke their sessions.
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
            foreach (var id in createdUsers)
            {
                var user = await users.FindByIdAsync(id.ToString());
                if (user is null) continue;
                user.IsActive = false;
                Success(await users.UpdateAsync(user));
                Success(await users.UpdateSecurityStampAsync(user));
            }
        }
    }

    private static WebApplicationFactory<ApiEntryPoint> CreateFactory(string connection) =>
        new WebApplicationFactory<ApiEntryPoint>().WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(Path.GetFullPath("backend/SangRental.Api"));
            builder.UseEnvironment("Development");
            ConfigureTestServices(builder, connection);
        });
    private static void ConfigureTestServices(IWebHostBuilder builder, string connection)
    {
        builder.UseSetting("Logging:LogLevel:Default", "None");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SangRentalDbContext>>();
            services.AddDbContext<SangRentalDbContext>(options => options.UseSqlServer(connection));
            var protection = services.AddDataProtection().PersistKeysToFileSystem(TestKeys);
            if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        });
    }
    private static HttpClient Client(WebApplicationFactory<ApiEntryPoint> factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    private static async Task<HttpResponseMessage> SubmitSsrAsync(HttpClient client, string path, string handler, Dictionary<string, string> values)
    {
        var html = await client.GetStringAsync(path);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!token.Success) throw new CheckFailedException("SSR form lacks antiforgery token");
        values["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        values["_handler"] = handler;
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }
    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password) =>
        await SendAsync(client, HttpMethod.Post, "/auth/login", new { username, password });
    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body, string? version = null)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/auth/csrf");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("requestToken").GetString());
        if (version is not null) request.Headers.Add("If-Match", '"' + version + '"');
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
    private static async Task<long> CreateAsync(HttpClient client, string path, object body)
    {
        using var response = await SendAsync(client, HttpMethod.Post, path, body);
        Status(response, HttpStatusCode.Created, "Created " + path);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
    }
    private static async Task<Guid> CreateUserAsync(UserManager<AdminUser> users, string name, string password, string role)
    {
        var user = new AdminUser { Id = Guid.NewGuid(), UserName = name, IsActive = true };
        Success(await users.CreateAsync(user, password));
        Success(await users.AddToRoleAsync(user, role));
        return user.Id;
    }
    private static void Success(IdentityResult result) { if (!result.Succeeded) throw new InvalidOperationException("Identity setup failed."); }
    private static void Check(bool value, string message) { if (!value) throw new CheckFailedException(message); checks++; }
    private static void Status(HttpResponseMessage response, HttpStatusCode status, string message) =>
        Check(response.StatusCode == status, message + ": unexpected HTTP status " + (int)response.StatusCode);
    private static async Task ExpectAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { checks++; return; }
        throw new InvalidOperationException("Expected rejection did not occur.");
    }
}

internal sealed class CheckFailedException(string message) : Exception(message);
