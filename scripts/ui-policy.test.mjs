import { test } from "node:test";
import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import { resolve, relative } from "node:path";
const root = resolve(import.meta.dirname, "..");
const read = path => readFileSync(resolve(root, path), "utf8");
function files(path) {
  return readdirSync(path, { withFileTypes: true }).flatMap(entry => entry.isDirectory() ? files(resolve(path, entry.name)) : [resolve(path, entry.name)]);
}
test("application code uses only the approved notification and icon libraries", () => {
  const pkg = JSON.parse(read("frontend/package.json"));
  const dependencies = { ...pkg.dependencies, ...pkg.devDependencies };
  for (const name of ["sonner", "react-hot-toast", "react-toastify", "lucide-react", "@radix-ui/react-toast"]) assert.equal(dependencies[name], undefined);
  assert.ok(dependencies.sweetalert2);
  assert.ok(dependencies["@fortawesome/react-fontawesome"]);
  const appFiles = ["frontend/src", "admin/SangRental.Admin/Components", "admin/SangRental.Admin/Services"].flatMap(path => files(resolve(root, path)));
  for (const file of appFiles.filter(file => /\.(tsx?|razor|cs|js)$/.test(file))) {
    const source = readFileSync(file, "utf8");
    const name = relative(root, file);
    assert.doesNotMatch(source, /\bSwal\.fire\s*\(|\b(?:window\.)?(?:alert|prompt)\s*\(|\bwindow\.confirm\s*\(/, name);
    assert.doesNotMatch(source, /library\.add\s*\(|import\s+\*\s+as\s+\w+\s+from\s+["']@fortawesome\/free-/, name);
  }
});
test("all six dictionaries have identical complete keys", () => {
  const messages = JSON.parse(read("frontend/src/messages/notifications.json"));
  assert.deepEqual(Object.keys(messages).sort(), ["en", "ja", "ko", "th", "vi", "zh-CN"]);
  for (const dictionary of Object.values(messages)) {
    assert.deepEqual(Object.keys(dictionary).sort(), Object.keys(messages.vi).sort());
    assert.ok(Object.values(dictionary).every(value => typeof value === "string" && value.trim().length > 0));
  }
});
test("admin uses the same design tokens and locale assets as Next", () => {
  assert.equal(read("admin/SangRental.Admin/wwwroot/vendor/coastal.css"), read("frontend/src/design/coastal.css"));
  assert.equal(read("admin/SangRental.Admin/wwwroot/vendor/notifications.json"), read("frontend/src/messages/notifications.json"));
  assert.match(read("admin/SangRental.Admin/Components/App.razor"), /vendor\/coastal\.css/);
  assert.match(read("frontend/src/app/globals.css"), /design\/coastal\.css/);
});
