/** Someone in the household who receives a daily milk delivery. */
export interface Recipient {
  id: string;
  name: string;
  /** Usual daily quantity, in whole millilitres. */
  expectedMl: number;
}

export type DeliveryStatus = "pending" | "delivered" | "missed";

/** What happened for one recipient on one day. */
export interface DeliveryEntry {
  recipientId: string;
  status: DeliveryStatus;
  /** Quantity actually received, in whole millilitres. Zero unless delivered. */
  deliveredMl: number;
}
