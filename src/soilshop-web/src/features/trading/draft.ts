export type DraftLine = { key: string; skuId: string; quantity: number; cost: string };
export const emptyLine = (): DraftLine => ({
  key: crypto.randomUUID(),
  skuId: "",
  quantity: 1,
  cost: "",
});
