"use client";

import { Notice } from "@/components/ui/notice";
import { getApiBaseUrl } from "@/services/api-client";
import { HouseholdCard } from "./household-card";
import { MilkRateCard } from "./milk-rate-card";

/**
 * The settings that live in the API: household and milk price. Without an API address there is nowhere to
 * save them, so say so instead of showing forms that cannot work.
 */
export function HouseholdSettings() {
  if (!getApiBaseUrl()) {
    return (
      <Notice title="Household and price settings need the API">
        This app is not connected to an API, so there is nowhere to save them. Run the API and point
        the web app at it (see docs/development.md).
      </Notice>
    );
  }

  return (
    <>
      <HouseholdCard />
      <MilkRateCard />
    </>
  );
}
