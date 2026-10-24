export interface PageResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
export interface User {
  id: number;
  username: string;
  displayName: string;
  role: "Admin" | "Employee";
}
export interface Login {
  token: string;
  expiresAt: string;
  user: User;
}
export interface Product {
  productType: string;
  id: number;
  code: string;
  name: string;
  brand: string | null;
  ingredients: string | null;
  sourceReference: string | null;
  isActive: boolean;
  version: number;
}
export const productTypes = [
  { code: "Unclassified", name: "Chưa phân loại" },
  { code: "GeneralPurpose", name: "Đất trồng đa dụng" },
  { code: "FoliageMix", name: "Đất trồng kiểng lá" },
  { code: "FloweringMix", name: "Đất trồng cây hoa" },
  { code: "SucculentMix", name: "Đất trồng sen đá / xương rồng" },
  { code: "OrchidMix", name: "Giá thể trồng lan" },
  { code: "SeedStartingMix", name: "Giá thể ươm hạt" },
] as const;
export const productTypeName = (code: string | undefined) =>
  productTypes.find((type) => type.code === code)?.name ?? "Chưa phân loại";
export interface Sku {
  id: number;
  productId: number;
  code: string;
  packageAmount: number;
  packageUnit: string;
  salePriceVnd: number;
  stockQty: number;
  stockVersion: number;
  lowStockThreshold: number;
  targetStock: number;
  preferredSupplierId: number | null;
  isActive: boolean;
  version: number;
}
export interface Supplier {
  id: number;
  code: string;
  name: string;
  contactNote: string | null;
  isActive: boolean;
}
export interface Trade {
  id: number;
  code: string;
  status: "Draft" | "Confirmed" | "Cancelled";
  supplierId: number | null;
  totalVnd: number | null;
  version: number;
  createdAt: string;
  lines: {
    skuId: number;
    quantity: number;
    unitPriceVnd: number | null;
    label: string | null;
  }[];
}
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
export const money = (value: number) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value);
export const time = (value: string) =>
  new Date(value).toLocaleString("en-GB", {
    timeZone: "Asia/Ho_Chi_Minh",
    dateStyle: "medium",
    timeStyle: "short",
  });
