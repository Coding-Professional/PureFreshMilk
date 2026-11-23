import type { Metadata } from "next";
import { TodayScreen } from "@/features/today/today-screen";
import { ConnectedDayScreen } from "@/features/today/connected-day-screen";
import { DEMO_RECIPIENTS } from "@/lib/demo-household";
import { getApiBaseUrl } from "@/services/api-client";

export const metadata: Metadata = { title: "Today" };

// Without an API address, the explicitly labelled demo remains available.
export default function TodayPage() {
  return getApiBaseUrl() ? (
    <ConnectedDayScreen date="today" />
  ) : (
    <TodayScreen recipients={DEMO_RECIPIENTS} />
  );
}
