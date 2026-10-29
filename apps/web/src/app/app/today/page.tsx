import type { Metadata } from "next";
import { TodayScreen } from "@/features/today/today-screen";
import { DEMO_RECIPIENTS } from "@/lib/demo-household";

export const metadata: Metadata = { title: "Today" };

// Demo data for now. When persistence lands, this page will load the household's
// recipients and today's entries from the API instead.
export default function TodayPage() {
  return <TodayScreen recipients={DEMO_RECIPIENTS} />;
}
