import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ConnectedDayScreen } from "@/features/today/connected-day-screen";
import { isIsoDate } from "@/lib/dates";

export const metadata: Metadata = { title: "Day record" };
// The date is a request-time route parameter; this page renders when navigated to.
export const instant = false;

export default async function DayRecordPage({ params }: { params: Promise<{ date: string }> }) {
  const { date } = await params;
  if (!isIsoDate(date)) notFound();
  return <ConnectedDayScreen date={date} />;
}
