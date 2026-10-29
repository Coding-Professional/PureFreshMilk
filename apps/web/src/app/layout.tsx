import type { Metadata, Viewport } from "next";
import type { ReactNode } from "react";
import { Gabarito, Lexend } from "next/font/google";
import "./globals.css";

// Body: Lexend, designed around reading fluency, with wide, open figures and a plain
// (unslashed) zero — quantities like "500 ml" and "1500 ml" must read at a glance.
const lexend = Lexend({
  subsets: ["latin"],
  variable: "--font-lexend",
  display: "swap",
});

// Display: Gabarito, used only for headings and headline numbers.
const gabarito = Gabarito({
  subsets: ["latin"],
  variable: "--font-gabarito",
  display: "swap",
});

export const metadata: Metadata = {
  title: { default: "Milk Manager", template: "%s · Milk Manager" },
  description: "A simple, exact record of your household's daily milk deliveries.",
  applicationName: "Milk Manager",
};

export const viewport: Viewport = {
  themeColor: "#f2f4f5",
  // Lets the bottom tab bar pad itself clear of the home indicator on notched phones.
  viewportFit: "cover",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="en" className={`${lexend.variable} ${gabarito.variable}`}>
      <body className="min-h-dvh antialiased">{children}</body>
    </html>
  );
}
