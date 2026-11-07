// Shared by Next.js and the Blazor JS adapter. Only translation keys reach the UI.
export function createNotifications(load, messages) {
    let locale = "vi";
    let active = null;
    let owner = 0;
    let engine = null;
    const text = (key) => {
        if (!Object.hasOwn(messages.vi, key))
            throw new Error("Unknown notification translation key.");
        return messages[locale]?.[key] ?? messages.vi[key];
    };
    const defaults = () => ({ confirmButtonText: text("ok"), cancelButtonText: text("cancel"), confirmButtonColor: "var(--coastal-ocean, #087EA4)", cancelButtonColor: "var(--coastal-navy, #123C50)", background: "var(--coastal-ivory, #FFF9EE)", color: "var(--coastal-navy, #123C50)", customClass: { popup: "sangrental-notification" }, returnFocus: true, showClass: { popup: "" }, hideClass: { popup: "" } });
    async function show(key, kind, toast = false) {
        if (active)
            return;
        active = "popup";
        try {
            engine = await load();
            await engine.fire({ ...defaults(), text: text(key), icon: kind, toast, position: toast ? "top-end" : "center", timer: toast ? 3500 : undefined, showConfirmButton: !toast });
        }
        finally {
            active = null;
        }
    }
    return {
        setLocale(value) { locale = Object.hasOwn(messages, value) ? value : "vi"; },
        success: (key) => show(key, "success"),
        error: (key) => show(key, "error"),
        warning: (key) => show(key, "warning"),
        info: (key) => show(key, "info"),
        toast: (key, kind = "info") => show(key, kind, true),
        async confirm(options) {
            if (active)
                return false;
            active = "popup";
            try {
                engine = await load();
                const result = await engine.fire({ ...defaults(), text: text(options.message), icon: "warning", showCancelButton: true, focusCancel: true, allowOutsideClick: false });
                return result.isConfirmed === true;
            }
            finally {
                active = null;
            }
        },
        async loading(key) {
            if (active)
                return null;
            active = "loading";
            const token = ++owner;
            try {
                engine = await load();
                void engine.fire({ ...defaults(), text: text(key), allowOutsideClick: false, allowEscapeKey: false, showConfirmButton: false, didOpen: () => engine?.showLoading() })
                    .catch(() => { if (active === "loading" && owner === token)
                    active = null; });
                return token;
            }
            catch (error) {
                active = null;
                throw error;
            }
        },
        closeLoading(token) {
            if (active !== "loading" || token !== owner)
                return;
            engine?.close();
            active = null;
        }
    };
}
