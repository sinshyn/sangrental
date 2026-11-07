import type { Metadata } from "next";
import "./globals.css";
import "sweetalert2/dist/sweetalert2.min.css";
import "@fortawesome/fontawesome-svg-core/styles.css";
import { SiteHeader } from "@/components/layout/site-header";
import { SiteFooter } from "@/components/layout/site-footer";
export const metadata: Metadata = { title: "SangRental | Thuê xe tại Đà Nẵng", description: "Nền tảng thuê xe máy và ô tô SangRental tại Đà Nẵng đang được xây dựng.", robots: { index: false, follow: false } };
export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
 return <html lang="vi"><body><a href="#main-content" className="sr-only focus:not-sr-only focus:block focus:p-4">Đến nội dung chính</a><SiteHeader />{children}<SiteFooter /></body></html>;
}
