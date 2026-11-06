import type { Metadata } from "../types";
import { levels, methods, notes, groups, contexts, physical } from "../options";
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
