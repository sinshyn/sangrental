export type Locale = "vi" | "en" | "ko" | "zh-CN" | "ja" | "th";
export type MessageKey = "ready" | "error" | "warning" | "info" | "confirm" | "loading";
type Kind = "success" | "error" | "warning" | "info";
type Dictionary = Record<Locale, Record<MessageKey | "ok" | "cancel", string>>;
export interface PopupEngine {
  fire(options: Record<string, unknown>): Promise<{ isConfirmed?: boolean }>;
  close(): void;
  showLoading(): void;
}

// Shared by Next.js and the Blazor JS adapter. Only translation keys reach the UI.
export function createNotifications(load: () => Promise<PopupEngine>, messages: Dictionary) {
  let locale: Locale = "vi";
  let active: "popup" | "loading" | null = null;
  let owner = 0;
  let engine: PopupEngine | null = null;
  const text = (key: MessageKey | "ok" | "cancel") => {
    if (!Object.hasOwn(messages.vi, key)) throw new Error("Unknown notification translation key.");
    return messages[locale]?.[key] ?? messages.vi[key];
  };
  const defaults = () => ({ confirmButtonText: text("ok"), cancelButtonText: text("cancel"), confirmButtonColor: "var(--coastal-ocean, #087EA4)", cancelButtonColor: "var(--coastal-navy, #123C50)", background: "var(--coastal-ivory, #FFF9EE)", color: "var(--coastal-navy, #123C50)", customClass: { popup: "sangrental-notification" }, returnFocus: true, showClass: { popup: "" }, hideClass: { popup: "" } });
  async function show(key: MessageKey, kind: Kind, toast = false) {
    if (active) return;
    active = "popup";
    try {
      engine = await load();
      await engine.fire({ ...defaults(), text: text(key), icon: kind, toast, position: toast ? "top-end" : "center", timer: toast ? 3500 : undefined, showConfirmButton: !toast });
    } finally { active = null; }
  }
  return {
    setLocale(value: string) { locale = Object.hasOwn(messages, value) ? value as Locale : "vi"; },
    success: (key: MessageKey) => show(key, "success"),
    error: (key: MessageKey) => show(key, "error"),
    warning: (key: MessageKey) => show(key, "warning"),
    info: (key: MessageKey) => show(key, "info"),
    toast: (key: MessageKey, kind: Kind = "info") => show(key, kind, true),
    async confirm(options: { message: MessageKey }) {
      if (active) return false;
      active = "popup";
      try {
        engine = await load();
        const result = await engine.fire({ ...defaults(), text: text(options.message), icon: "warning", showCancelButton: true, focusCancel: true, allowOutsideClick: false });
        return result.isConfirmed === true;
      } finally { active = null; }
    },
    async loading(key: MessageKey) {
      if (active) return null;
      active = "loading";
      const token = ++owner;
      try {
        engine = await load();
        void engine.fire({ ...defaults(), text: text(key), allowOutsideClick: false, allowEscapeKey: false, showConfirmButton: false, didOpen: () => engine?.showLoading() })
          .catch(() => { if (active === "loading" && owner === token) active = null; });
        return token;
      } catch (error) { active = null; throw error; }
    },
    closeLoading(token: number) {
      if (active !== "loading" || token !== owner) return;
      engine?.close();
      active = null;
    }
  };
}
