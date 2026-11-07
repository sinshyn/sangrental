import type { ComponentProps } from "react";
import { cn } from "@/lib/utils";
export function Button({ className, type = "button", ...props }: ComponentProps<"button">) {
  return <button type={type} className={cn("inline-flex min-h-11 items-center justify-center rounded-xl bg-primary px-5 py-3 text-sm font-semibold text-primary-foreground transition-colors hover:opacity-90 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-primary disabled:cursor-not-allowed disabled:opacity-50", className)} {...props} />;
}
