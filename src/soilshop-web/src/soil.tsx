import type { Sku } from "./types";
export const levels = { Low: "Thấp", Medium: "Vừa", High: "Cao" };
export const groups = {
  General: "Đa dụng",
  Foliage: "Kiểng lá",
  Flowering: "Cây hoa",
  Succulent: "Sen đá / xương rồng",
  Orchid: "Lan",
  Seedling: "Ươm hạt",
  Bonsai: "Bonsai",
  AcidLoving: "Cây ưa axit",
};
export const contexts = {
  Pot: "Trồng chậu",
  InGround: "Trồng trực tiếp",
  Both: "Cả hai",
};
export const methods = {
  SME: "SME",
  Extract1to2: "Chiết 1:2",
  PourThrough: "PourThrough",
  Other: "Khác (không so sánh)",
};
export const notes = {
  structureNotes: "Cấu trúc và độ bền",
  nutritionNotes: "Dinh dưỡng / NPK",
  organicMatterNotes: "Chất hữu cơ",
  biologyNotes: "Vi sinh",
  cleanlinessNotes: "Độ sạch / xử lý",
};
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
export const physical = {
  drainage: "Thoát nước",
  aeration: "Thoáng khí",
  waterRetention: "Giữ ẩm",
};
export function Options({ values }: { values: Record<string, string> }) {
  return (
    <>
      <option value="">Chưa xác định / không chọn</option>
      {Object.entries(values).map(([v, label]) => (
        <option key={v} value={v}>
          {label}
        </option>
      ))}
    </>
  );
}
export function SoilFacts({ data: d }: { data: Metadata }) {
  const label = (values: Record<string, string>, v: string | null) =>
    v ? (values[v] ?? v) : "Chưa có thông tin";
  return (
    <dl className="soil-facts">
      {Object.entries(physical).map(([key, name]) => (
        <div key={key}>
          <dt>{name}</dt>
          <dd>{label(levels, d[key as keyof typeof physical])}</dd>
        </div>
      ))}
      <div>
        <dt>pH</dt>
        <dd>
          {d.phMin === null ? "Chưa có thông tin" : `${d.phMin} – ${d.phMax}`}
        </dd>
      </div>
      <div>
        <dt>EC (mS/cm)</dt>
        <dd>
          {d.ecMin === null
            ? "Chưa có thông tin"
            : `${d.ecMin} – ${d.ecMax} · ${label(methods, d.ecMethod)}`}
        </dd>
      </div>
      {Object.entries(notes).map(([key, name]) => (
        <div key={key}>
          <dt>{name}</dt>
          <dd>{d[key as keyof typeof notes] || "Chưa có thông tin"}</dd>
        </div>
      ))}
      <div>
        <dt>Thành phần</dt>
        <dd>{d.ingredients || "Chưa có thông tin"}</dd>
      </div>
      <div>
        <dt>Nhóm cây</dt>
        <dd>
          {d.plantGroups.map((v) => label(groups, v)).join(", ") ||
            "Chưa có thông tin"}
        </dd>
      </div>
      <div>
        <dt>Vị trí trồng</dt>
        <dd>{label(contexts, d.useContext)}</dd>
      </div>
      <div>
        <dt>Có mẫu thử</dt>
        <dd>
          {d.sampleAvailable === null
            ? "Chưa có thông tin"
            : d.sampleAvailable
              ? "Có"
              : "Không"}
        </dd>
      </div>
      <div>
        <dt>Nguồn</dt>
        <dd>{d.sourceReference || "Chưa có nguồn"}</dd>
      </div>
      <div>
        <dt>Duyệt thông tin</dt>
        <dd>
          {d.reviewedAt
            ? `${new Date(d.reviewedAt).toLocaleString("vi-VN")} · nhân viên #${d.reviewedBy}`
            : "Chưa duyệt"}{" "}
          · phiên bản {d.version}
        </dd>
      </div>
    </dl>
  );
}
