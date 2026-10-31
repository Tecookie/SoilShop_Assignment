import { useState } from "react";
import type { FormEvent } from "react";
import { api } from "./api";
import { Field, Notice } from "./components";
import {
  contexts,
  groups,
  levels,
  methods,
  Options,
  physical,
  SoilFacts,
} from "./soil";
import type { AdviceResponse } from "./soil";
import { money, time } from "./types";
export function AdvicePage({ onSale }: { onSale: () => void }) {
  const [result, setResult] = useState<AdviceResponse>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function search(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    const body: Record<string, unknown> = {};
    for (const k of [
      "plantGroup",
      ...Object.keys(physical),
      "ecMethod",
      "useContext",
      "packageUnit",
    ])
      body[k] = f.get(k) || null;
    for (const k of ["phMin", "phMax", "maxEc", "maxPriceVnd", "packageAmount"])
      body[k] = f.get(k) === "" ? null : Number(f.get(k));
    body.allowIncomplete = f.has("allowIncomplete");
    body.requireSample = f.has("requireSample");
    body.requiredInformation = f.getAll("requiredInformation");
    setBusy(true);
    setError("");
    setResult(undefined);
    try {
      setResult(await api<AdviceResponse>("/product-advice", body));
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function draft(skuId: number) {
    setBusy(true);
    setError("");
    try {
      await api("/sales", { lines: [{ skuId, quantity: 1 }] });
      onSale();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">TƯ VẤN TẠI QUẦY</p>
          <h1>Tìm đất phù hợp</h1>
          <p>
            Chọn nhu cầu của khách. Ưu tiên cân bằng thoát nước, thoáng khí và
            giữ ẩm; không tự mặc định càng cao càng tốt.
          </p>
        </div>
      </div>
      <section className="panel form-panel">
        <form onSubmit={search}>
          <div className="form-grid">
            <Field label="Nhóm cây">
              <select name="plantGroup">
                <Options values={groups} />
              </select>
            </Field>
            {Object.entries(physical).map(([key, label]) => (
              <Field key={key} label={label}>
                <select name={key}>
                  <Options values={levels} />
                </select>
              </Field>
            ))}
            {[
              ["phMin", "pH tối thiểu", 14, "0.01"],
              ["phMax", "pH tối đa", 14, "0.01"],
              ["maxEc", "EC tối đa (mS/cm)", 99999.999, "0.001"],
              ["maxPriceVnd", "Ngân sách mỗi bao (VND)", 1000000000000, "1"],
              ["packageAmount", "Khối lượng / thể tích bao", 1000000, "0.01"],
            ].map(([key, label, max, step]) => (
              <Field key={key} label={String(label)}>
                <input
                  name={String(key)}
                  type="number"
                  min={
                    key === "maxPriceVnd"
                      ? "1"
                      : key === "packageAmount"
                        ? "0.01"
                        : "0"
                  }
                  max={max}
                  step={step}
                />
              </Field>
            ))}
            <Field label="Phương pháp đo EC">
              <select name="ecMethod">
                <Options
                  values={Object.fromEntries(
                    Object.entries(methods).filter(([k]) => k !== "Other"),
                  )}
                />
              </select>
            </Field>
            <Field label="Vị trí trồng">
              <select name="useContext">
                <Options values={contexts} />
              </select>
            </Field>
            <Field label="Đơn vị bao">
              <select name="packageUnit">
                <Options values={{ kg: "kg", L: "L" }} />
              </select>
            </Field>
          </div>
          <p className="muted">
            pH cần cả hai giới hạn. EC chỉ so sánh cùng phương pháp đo. Không có
            ngưỡng tự động cho từng nhóm cây.
          </p>
          <fieldset>
            <legend>
              Bắt buộc có thông tin từ nguồn (không phải đánh giá chất lượng)
            </legend>
            {Object.entries({
              Structure: "Cấu trúc",
              Nutrition: "Dinh dưỡng",
              Organic: "Hữu cơ",
              Biology: "Vi sinh",
              Cleanliness: "Độ sạch",
            }).map(([value, label]) => (
              <label key={value} className="soil-check">
                <input
                  type="checkbox"
                  name="requiredInformation"
                  value={value}
                />
                {label}
              </label>
            ))}
          </fieldset>
          <label className="soil-check">
            <input type="checkbox" name="requireSample" />
            Có mẫu thử
          </label>
          <label className="soil-check">
            <input type="checkbox" name="allowIncomplete" />
            Cho phép thiếu một số tiêu chí (hiển thị rõ, không cộng điểm)
          </label>
          <p>
            Chỉ xét sản phẩm đang bán, còn tồn và có thông tin đã duyệt. Dữ liệu
            mâu thuẫn yêu cầu luôn bị loại.
          </p>
          <button disabled={busy}>
            {busy ? "Đang xử lý…" : "Tìm sản phẩm"}
          </button>
        </form>
      </section>
      <Notice error={error} />
      {result && (
        <section aria-live="polite">
          <h2>
            {result.results.length
              ? "Sản phẩm khớp yêu cầu"
              : "Chưa tìm thấy sản phẩm"}
          </h2>
          <p>{result.message}</p>
          <p className="muted">
            {time(result.generatedAt)} · {result.ruleVersion} · xét{" "}
            {result.candidatesConsidered} SKU
          </p>
          {result.results.map(
            ({ candidate: c, score, maxScore, reasons, missing }) => (
              <article className="panel advice-card" key={c.sku.id}>
                <h3>{c.productName}</h3>
                <p>
                  {c.sku.code} · {c.sku.packageAmount} {c.sku.packageUnit} ·{" "}
                  {money(c.sku.salePriceVnd)} / bao · tồn {c.sku.stockQty} bao
                  tại lúc tra cứu
                </p>
                <strong>
                  {score}/{maxScore} điểm khớp tiêu chí
                </strong>
                <ul>
                  {reasons.map((r) => (
                    <li key={r}>{r}</li>
                  ))}
                </ul>
                {missing.length > 0 && (
                  <>
                    <h4>Thông tin còn thiếu / không so sánh</h4>
                    <ul>
                      {missing.map((m) => (
                        <li key={m}>{m}</li>
                      ))}
                    </ul>
                  </>
                )}
                <details>
                  <summary>Chín yếu tố, thành phần và nguồn</summary>
                  <SoilFacts data={c.metadata} />
                </details>
                <p>
                  Tư vấn không giữ hàng. Xác nhận phiếu bán sẽ kiểm tra lại giá
                  và tồn kho.
                </p>
                <button disabled={busy} onClick={() => draft(c.sku.id)}>
                  Tạo phiếu bán nháp · 1 bao
                </button>
              </article>
            ),
          )}
        </section>
      )}
    </>
  );
}
