import { spawn, execFileSync } from "node:child_process";
import { setTimeout as delay } from "node:timers/promises";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const api = resolve(root, "backend/SangRental.Api");
const requireReady = process.argv.includes("--require-ready");
async function check(name, port, env, expectedReady) {
  const child = spawn("dotnet", [resolve(api, "bin/Debug/net10.0/SangRental.Api.dll"), "--urls", `http://127.0.0.1:${port}`, "--environment", "Development"], {
    cwd: api, env: { ...process.env, ...env }, windowsHide: true,
    // Never echo application logs or configuration; health responses are generic text only.
    stdio: "ignore"
  });
  let spawnFailed = false;
  child.on("error", () => { spawnFailed = true; });
  const request = path => fetch(`http://127.0.0.1:${port}${path}`, { signal: AbortSignal.timeout(20000) });
  try {
    let started = false;
    for (let i = 0; i < 40; i++) {
      if (spawnFailed || child.exitCode !== null) throw new Error(`${name}: API startup failed.`);
      try { if ((await request("/health/live")).status === 200) { started = true; break; } } catch { /* Startup delay. */ }
      await delay(250);
    }
    if (!started) throw new Error(`${name}: API startup timed out.`);
    const response = await request("/health/ready");
    const body = await response.text();
    if (![200, 503].includes(response.status) || body !== (response.status === 200 ? "Healthy" : "Unhealthy"))
      throw new Error(`${name}: unexpected or non-generic readiness response.`);
    if (expectedReady !== null && response.status !== expectedReady)
      throw new Error(`${name}: expected HTTP ${expectedReady}, got ${response.status}.`);
    console.log(`${response.status === 200 || expectedReady === 503 ? "PASS" : "NOT VERIFIED"} ${name}: live 200, ready ${response.status}, generic body.`);
    return response.status;
  } finally {
    if (child.pid && child.exitCode === null) {
      if (process.platform === "win32") {
        execFileSync("taskkill", ["/PID", String(child.pid), "/T", "/F"], { windowsHide: true, stdio: "ignore" });
      } else child.kill("SIGTERM");
    }
  }
}
try {
  const configured = await check("configured-database", 15310, {}, null);
  await check("missing-configuration", 15311, { ConnectionStrings__SangRental: "" }, 503);
  await check("unreachable-database", 15312, { ConnectionStrings__SangRental: "Server=127.0.0.1,1;Database=SangRentalDb;Encrypt=True;TrustServerCertificate=True;Connect Timeout=1;" }, 503);
  if (requireReady && configured !== 200) throw new Error("Configured database connection was not verified. Set User Secrets and rerun.");
} catch (error) {
  // Only controlled diagnostics; never print exception details originating in SQL/configuration.
  console.error(error instanceof Error ? error.message : "Database smoke failed.");
  process.exitCode = 1;
}
