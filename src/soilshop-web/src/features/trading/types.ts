export interface Trade {
  id: number;
  code: string;
  status: "Draft" | "Confirmed" | "Cancelled";
  supplierId: number | null;
  totalVnd: number | null;
  version: number;
  createdAt: string;
  confirmedBy: number | null;
  confirmedAt: string | null;
  cancelledBy: number | null;
  cancelledAt: string | null;
  cancelReason: string | null;
  lines: {
    skuId: number;
    quantity: number;
    unitPriceVnd: number | null;
    label: string | null;
  }[];
}

export interface TradeQuote {
  version: number;
  totalVnd: number;
  lines: { skuId: number; quantity: number; unitPriceVnd: number }[];
}
