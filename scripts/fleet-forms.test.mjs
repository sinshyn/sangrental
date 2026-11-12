import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import vm from "node:vm";

const source = readFileSync(new URL("../admin/SangRental.Admin/wwwroot/fleet-forms.js", import.meta.url), "utf8")
    .replace('import { invoke } from "./notifications.js";', "");

function harness(invoke, sensitive = false) {
    let submit, pageshow;
    const forms = [];
    class Form {
        dataset = { confirm: sensitive ? "true" : "false" };
        button = { disabled: false };
        boundary = { hidden: true };
        submissions = 0;
        matches() { return true; }
        reportValidity() { return true; }
        setAttribute() { }
        removeAttribute() { }
        querySelector(selector) { return selector === "[data-notification-failure]" ? this.boundary : null; }
        querySelectorAll() { return [this.button]; }
        requestSubmit() {
            const event = eventFor(this);
            submit(event);
            if (!event.prevented) this.submissions++;
        }
    }
    vm.runInNewContext(source, {
        invoke, HTMLFormElement: Form,
        document: { addEventListener: (_, fn) => { submit = fn; }, querySelectorAll: () => forms },
        window: { addEventListener: (_, fn) => { pageshow = fn; } },
    });
    const form = new Form(); forms.push(form);
    return { form, submit: () => submit(eventFor(form)), restore: () => pageshow() };
}

function eventFor(target) { return { target, prevented: false, preventDefault() { this.prevented = true; } }; }

test("fleet confirmation cancel does not submit or open loading", async () => {
    const calls = [];
    const h = harness(async method => { calls.push(method); return false; }, true);
    await h.submit();
    assert.deepEqual(calls, ["confirm"]);
    assert.equal(h.form.submissions, 0);
    assert.equal(h.form.dataset.pending, "false");
});

test("fleet submit locks repeated clicks and Enter until navigation", async () => {
    let release;
    const h = harness(() => new Promise(resolve => { release = resolve; }));
    const first = h.submit();
    await h.submit();
    release(1);
    await first;
    await h.submit();
    assert.equal(h.form.submissions, 1);
    assert.equal(h.form.button.disabled, true);
    assert.equal(h.form.dataset.pending, "true");
    h.restore();
    assert.equal(h.form.button.disabled, false);
    assert.equal(h.form.dataset.pending, "false");
});

test("fleet notification failure blocks submission and permits retry", async () => {
    let fail = true;
    const h = harness(async () => { if (fail) throw Error("offline"); return 1; });
    await h.submit();
    assert.equal(h.form.submissions, 0);
    assert.equal(h.form.boundary.hidden, false);
    assert.equal(h.form.dataset.pending, "false");
    fail = false;
    await h.submit();
    assert.equal(h.form.submissions, 1);
});

test("fleet sensitive action confirms before loading and submitting", async () => {
    const calls = [];
    const h = harness(async method => { calls.push(method); return true; }, true);
    await h.submit();
    assert.deepEqual(calls, ["confirm", "loading"]);
    assert.equal(h.form.submissions, 1);
});
