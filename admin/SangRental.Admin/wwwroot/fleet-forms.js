import { invoke } from "./notifications.js";

// Native SSR navigation keeps a fresh authorized HTTP request for every write.
const approved = new WeakSet();
const submitted = new WeakSet();
document.addEventListener("submit", async event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.matches("[data-fleet-form]")) return;
    if (submitted.has(form)) { event.preventDefault(); return; }
    if (approved.has(form)) {
        approved.delete(form);
        submitted.add(form);
        return;
    }
    event.preventDefault();
    if (form.dataset.pending === "true" || !form.reportValidity()) return;
    form.dataset.pending = "true";
    form.setAttribute("aria-busy", "true");
    try {
        const sensitive = form.dataset.confirm === "true"
            || form.querySelector('[name="Input.ConfirmInitialInspection"]')?.checked
            || form.querySelector('[name="Input.Status"]')?.value === "Skipped";
        if (sensitive && !await invoke("confirm", { message: "confirm" }, "vi")) return;
        await invoke("loading", { message: "loading" }, "vi");
        approved.add(form);
        form.requestSubmit();
        form.querySelectorAll('button[type="submit"]').forEach(button => { button.disabled = true; });
    } catch {
        // Fail closed if the notification adapter cannot load; the inline boundary works without it.
        const boundary = form.querySelector("[data-notification-failure]");
        if (boundary) boundary.hidden = false;
    } finally {
        if (!submitted.has(form)) {
            form.dataset.pending = "false";
            form.removeAttribute("aria-busy");
        }
    }
}, true);

window.addEventListener("pageshow", () => {
    document.querySelectorAll("form[data-fleet-form]").forEach(form => {
        form.dataset.pending = "false";
        approved.delete(form);
        submitted.delete(form);
        form.removeAttribute("aria-busy");
        form.querySelectorAll('button[type="submit"]').forEach(button => { button.disabled = false; });
    });
});
