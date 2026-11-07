import { createNotifications } from "./vendor/notification-core.js";
let service;
let initialization;
export async function initialize() {
    initialization ??= (async () => {
        const response = await fetch(new URL("./vendor/notifications.json", import.meta.url));
        if (!response.ok) throw new Error("Notification resources are unavailable.");
        service = createNotifications(async () => {
            if (!globalThis.Swal?.fire) throw new Error("Notification engine is unavailable.");
            return globalThis.Swal;
        }, await response.json());
    })().catch(error => { initialization = undefined; throw error; });
    await initialization;
}
export async function invoke(method, payload, locale) {
    await initialize();
    service.setLocale(locale);
    switch (method) {
        case "success": case "error": case "warning": case "info": return service[method](payload.message);
        case "confirm": return service.confirm({ message: payload.message });
        case "loading": return service.loading(payload.message);
        case "closeLoading": return service.closeLoading(payload.token);
        case "toast": return service.toast(payload.message, payload.type);
        default: throw new Error("Unsupported notification operation.");
    }
}
