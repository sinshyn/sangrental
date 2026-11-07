import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { createNotifications } from "../frontend/src/lib/notification-core.ts";
const messages = JSON.parse(readFileSync(new URL("../frontend/src/messages/notifications.json", import.meta.url)));
function fixture() {
  const calls = [];
  let response = { isConfirmed: false };
  let closed = 0;
  const engine = { fire: async options => { calls.push(options); return response; }, close: () => closed++, showLoading() {} };
  return { service: createNotifications(async () => engine, messages), calls, answer: value => response = value, closed: () => closed };
}
test("confirm/cancel returns explicit result without a business action", async () => {
  const f = fixture();
  assert.equal(await f.service.confirm({ message: "confirm" }), false);
  f.answer({ isConfirmed: true });
  assert.equal(await f.service.confirm({ message: "confirm" }), true);
  assert.equal(f.calls[0].focusCancel, true);
  assert.equal(f.calls[0].allowOutsideClick, false);
});
test("six locales and fallback use text, never untrusted HTML", async () => {
  const f = fixture();
  for (const locale of ["vi", "en", "ko", "zh-CN", "ja", "th", "unknown"]) {
    f.service.setLocale(locale);
    await f.service.info("info");
    const options = f.calls.at(-1);
    assert.equal(options.text, messages[locale in messages ? locale : "vi"].info);
    assert.equal(options.html, undefined);
    assert.ok(options.confirmButtonText);
  }
});
test("loading blocks duplicate requests and only its owner closes it", async () => {
  const f = fixture();
  const token = await f.service.loading("loading");
  assert.equal(await f.service.confirm({ message: "confirm" }), false);
  await f.service.error("error");
  assert.equal(await f.service.loading("loading"), null);
  f.service.closeLoading(token + 1);
  assert.equal(f.closed(), 0);
  f.service.closeLoading(token);
  assert.equal(f.closed(), 1);
  await f.service.info("info");
  assert.equal(f.calls.length, 2);
  assert.equal(f.calls[0].allowEscapeKey, false);
});
test("all notification styles route through one engine", async () => {
  const f = fixture();
  for (const kind of ["success", "error", "warning", "info"]) {
    await f.service[kind](kind === "success" ? "ready" : kind);
    assert.equal(f.calls.at(-1).icon, kind);
  }
  await f.service.toast("info", "info");
  assert.equal(f.calls.at(-1).toast, true);
});
test("concurrent confirm is suppressed and failure releases the lock", async () => {
  let fail = true;
  const service = createNotifications(async () => { if (fail) throw Error("network"); return { fire: async () => ({ isConfirmed: true }), close() {}, showLoading() {} }; }, messages);
  await assert.rejects(service.info("info"), /network/);
  fail = false;
  const pending = service.confirm({ message: "confirm" });
  assert.equal(await service.confirm({ message: "confirm" }), false);
  assert.equal(await pending, true);
});

test("unknown translation keys fail safely without displaying raw input", async () => {
  const f = fixture();
  await assert.rejects(f.service.info("customer-sensitive-value"), /Unknown notification translation key/);
  assert.equal(f.calls.length, 0);
  await f.service.info("info");
  assert.equal(f.calls.length, 1);
});

test("rejected loading popup releases its lock without an unhandled rejection", async () => {
  let calls = 0;
  const service = createNotifications(async () => ({ fire: async () => { if (++calls === 1) throw Error("renderer failed"); return {}; }, close() {}, showLoading() {} }), messages);
  await service.loading("loading");
  await new Promise(resolve => setImmediate(resolve));
  await service.info("info");
  assert.equal(calls, 2);
});

test("a stale loading failure cannot unlock a newer operation", async () => {
  let rejectFirst;
  let calls = 0;
  const service = createNotifications(async () => ({ fire: () => ++calls === 1 ? new Promise((_, reject) => { rejectFirst = reject; }) : new Promise(() => {}), close() {}, showLoading() {} }), messages);
  const first = await service.loading("loading");
  service.closeLoading(first);
  const second = await service.loading("loading");
  rejectFirst(Error("late rejection"));
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(await service.confirm({ message: "confirm" }), false);
  service.closeLoading(second);
});
