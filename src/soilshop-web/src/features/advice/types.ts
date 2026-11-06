import type { Sku } from "../catalogue/types";
export interface Metadata {
  productId: number;
  version: number;
  drainage: string | null;
  aeration: string | null;
  waterRetention: string | null;
  phMin: number | null;
  phMax: number | null;
  ecMin: number | null;
  ecMax: number | null;
  ecMethod: string | null;
  useContext: string | null;
  sampleAvailable: boolean | null;
  structureNotes: string | null;
  nutritionNotes: string | null;
  organicMatterNotes: string | null;
  biologyNotes: string | null;
  cleanlinessNotes: string | null;
  ingredients: string | null;
  sourceReference: string | null;
  reviewedAt: string | null;
  reviewedBy: number | null;
  plantGroups: string[];
}

export interface AdviceResponse {
  generatedAt: string;
  ruleVersion: string;
  candidatesConsidered: number;
  message: string;
  results: {
    candidate: { sku: Sku; productName: string; metadata: Metadata };
    score: number;
    maxScore: number;
    reasons: string[];
    missing: string[];
  }[];
}
