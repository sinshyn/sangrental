import { spawn, execFileSync } from "node:child_process";
import { setTimeout as delay } from "node:timers/promises";
import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const children = [];
const results = [];
// Isolate smoke keys from real development sessions and the user's profile key ring.
const keyDirectory = resolve(root, "artifacts", `smoke-keys-${process.pid}-${Date.now()}`);
mkdirSync(keyDirectory, { recursive: true });
const smokeEnvironment = { ConnectionStrings__SangRental: "", DataProtection__KeyDirectory: keyDirectory };
function start(command, args, env = {}) {
  const child = spawn(command, args, { cwd: root, env: { ...process.env, ...env }, windowsHide: true, stdio: ["ignore", "pipe", "pipe"] });
  child.log = "";
  child.stdout.on("data", data => { child.log = (child.log + data).slice(-6000); });
  child.stderr.on("data", data => { child.log = (child.log + data).slice(-6000); });
  child.on("error", error => { child.failure = error; });
  children.push(child);
  return child;
}
async function request(url) { return fetch(url, { redirect: "manual", signal: AbortSignal.timeout(5000) }); }
async function ready(child, url) {
  for (let i = 0; i < 60; i++) {
    if (child.failure) throw child.failure;
    if (child.exitCode !== null) throw new Error(`Process exited: ${child.log}`);
    try { if ((await request(url)).ok) return; } catch { /* Startup may still be in progress. */ }
    await delay(500);
  }
  throw new Error(`Startup timeout: ${url}\n${child.log}`);
}
async function check(name, url, status, validate = () => true) {
  const response = await request(url);
  const body = await response.text();
  if (response.status !== status || !validate(body, response)) {
    const diagnostic = response.status === 500 ? body.match(/\b(?:[A-Za-z]+\.)*[A-Za-z]+Exception\b/)?.[0] : null;
    throw new Error(`${name}: expected ${status}, got ${response.status}${diagnostic ? ` (${diagnostic})` : ""}`);
  }
  results.push({ name, status, result: "PASS" });
  console.log(`PASS ${name} (${status})`);
}
try {
  // No SQL connection is allowed during scaffold smoke tests.
  const api = start("dotnet", ["backend/SangRental.Api/bin/Debug/net10.0/SangRental.Api.dll", "--contentRoot", resolve(root, "backend/SangRental.Api"), "--urls", "http://127.0.0.1:15100", "--environment", "Development"], smokeEnvironment);
  const admin = start("dotnet", ["admin/SangRental.Admin/bin/Debug/net10.0/SangRental.Admin.dll", "--contentRoot", resolve(root, "admin/SangRental.Admin"), "--urls", "http://127.0.0.1:15200", "--environment", "Development"], smokeEnvironment);
  const web = start(process.execPath, ["frontend/node_modules/next/dist/bin/next", "start", "frontend", "--hostname", "127.0.0.1", "--port", "13000"]);
  await Promise.all([ready(api, "http://127.0.0.1:15100/health/live"), ready(admin, "http://127.0.0.1:15200/health/live"), ready(web, "http://127.0.0.1:13000")]);
  await check("api-live", "http://127.0.0.1:15100/health/live", 200);
  await check("api-ready-without-db", "http://127.0.0.1:15100/health/ready", 503, body => body === "Unhealthy");
  await check("api-openapi", "http://127.0.0.1:15100/openapi/v1.json", 200, body => Boolean(JSON.parse(body).openapi));
  await check("api-problem-details", "http://127.0.0.1:15100/missing", 404, (body, response) => response.headers.get("content-type")?.includes("application/problem+json") && JSON.parse(body).status === 404);
  await check("admin-live-without-db", "http://127.0.0.1:15200/health/live", 200, body => body === "Healthy");
  await check("admin-ready-without-db", "http://127.0.0.1:15200/health/ready", 503, body => body === "Unhealthy");
  await check("admin-login-requires-https", "http://127.0.0.1:15200/account/login", 200, body => body.includes("HTTPS") && body.includes("mật khẩu mặc định"));
  await check("admin-dashboard-protected", "http://127.0.0.1:15200/", 302, (_, response) => response.headers.get("location")?.includes("/account/login"));
  await check("admin-fleet-protected", "http://127.0.0.1:15200/fleet", 302, (_, response) => response.headers.get("location")?.includes("/account/login"));
  await check("admin-catalog-images-protected", "http://127.0.0.1:15200/catalog/models/1/images", 302, (_, response) => response.headers.get("location")?.includes("/account/login"));
  await check("api-catalog-images-protected", "http://127.0.0.1:15100/api/admin/catalog/models/1/images", 401);
  await check("admin-api-protected", "http://127.0.0.1:15200/auth/session", 401);
  await check("admin-notification-module", "http://127.0.0.1:15200/notifications.js", 200, body => body.includes("createNotifications"));
  await check("admin-fleet-form-module", "http://127.0.0.1:15200/fleet-forms.js", 200, body => body.includes("requestSubmit") && body.includes("notifications.js"));
  await check("admin-notification-locales", "http://127.0.0.1:15200/vendor/notifications.json", 200, body => Object.keys(JSON.parse(body)).length === 6);
  await check("admin-sweetalert-assets", "http://127.0.0.1:15200/vendor/sweetalert2.all.min.js", 200, body => body.includes("SweetAlert2"));
  await check("admin-coastal-theme", "http://127.0.0.1:15200/vendor/coastal.css", 200, body => body.includes("--coastal-ivory") && body.includes("sangrental-notification"));
  await check("frontend-production", "http://127.0.0.1:13000/", 200, body => body.includes('lang="vi"') && body.includes("Chưa tiếp nhận đặt xe") && !body.includes("Kh?m ph?"));
} catch (error) {
  console.error(error);
  process.exitCode = 1;
} finally {
  for (const child of children.reverse()) {
    if (!child.pid || child.exitCode !== null) continue;
    if (process.platform === "win32") {
      try { execFileSync("taskkill", ["/PID", String(child.pid), "/T", "/F"], { windowsHide: true, stdio: "ignore", timeout: 5000 }); } catch { child.kill(); }
    } else child.kill("SIGTERM");
  }
  mkdirSync(resolve(root, "artifacts"), { recursive: true });
  writeFileSync(resolve(root, "artifacts/smoke-results.json"), JSON.stringify({ passed: !process.exitCode, results }, null, 2));
}
