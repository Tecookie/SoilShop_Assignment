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
