import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faCompass } from "@fortawesome/free-solid-svg-icons";
import { config } from "@fortawesome/fontawesome-svg-core";
config.autoAddCss = false;
export function SiteHeader() {
 return <header className="border-b border-border bg-white/95"><div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-5"><a href="/" className="text-2xl font-extrabold tracking-tight">Sang<span className="text-primary">Rental</span><span className="mt-1 block text-[10px] font-semibold tracking-[0.25em] text-muted-foreground">ĐÀ NẴNG · XE MÁY & Ô TÔ</span></a><a href="#vehicle-categories" className="rounded-lg px-3 py-2 text-sm font-semibold text-primary focus-visible:outline-2">Khám phá danh mục <FontAwesomeIcon icon={faCompass} aria-hidden="true" className="ml-2 size-4" /></a></div></header>;
}
