export interface Movement {
  id: number;
  skuId: number;
  kind: string;
  deltaQty: number;
  balanceAfter: number;
  stockVersionAfter: number;
  createdAt: string;
}

export interface Suggestion {
  id: number;
  skuId: number;
  supplierId: number | null;
  observedQty: number;
  suggestedQty: number;
  status: string;
  updatedAt: string;
}
