import type { Metadata } from "next";
import { RecipientsScreen } from "@/features/recipients/recipients-screen";

export const metadata: Metadata = { title: "Recipients" };

export default function RecipientsPage() {
  return <RecipientsScreen />;
}
