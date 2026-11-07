"use client";
import { createNotifications, type PopupEngine } from "./notification-core";
import messages from "../messages/notifications.json";

export const NotificationService = createNotifications(async () => {
  if (typeof window === "undefined") throw new Error("Notifications require an interactive browser.");
  const { default: Swal } = await import("sweetalert2");
  return Swal as unknown as PopupEngine;
}, messages);
