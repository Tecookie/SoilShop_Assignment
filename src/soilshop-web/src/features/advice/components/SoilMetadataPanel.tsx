import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { api } from "../../../shared/api/client";
import { Field, Notice } from "../../../shared/components";
import { contexts, groups, levels, methods, notes, physical } from "../options";
import { Options } from "./Options";
import { SoilFacts } from "./SoilFacts";
import type { Metadata } from "../types";
export function SoilMetadataPanel({
  productId,
  onChanged,
  onClose,
}: {
  productId: number;
  onChanged: () => Promise<void>;
  onClose: () => void;
}) {
  const [dirty, setDirty] = useState(false);
  const [data, setData] = useState<Metadata>();
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    api<Metadata>(`/products/${productId}/metadata`)
      .then(setData)
      .catch((e) => setError(e.message));
  }, [productId]);
  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!data) return;
    const f = new FormData(e.currentTarget);
    const body: Record<string, unknown> = {
      expectedVersion: data.version,
      plantGroups: f.getAll("plantGroups"),
    };
    for (const key of [
      ...Object.keys(physical),
      ...Object.keys(notes),
      "ingredients",
      "sourceReference",
      "ecMethod",
      "useContext",
    ])
      body[key] = f.get(key) || null;
    for (const key of ["phMin", "phMax", "ecMin", "ecMax"])
      body[key] = f.get(key) === "" ? null : Number(f.get(key));
    body.sampleAvailable =
      f.get("sampleAvailable") === ""
        ? null
        : f.get("sampleAvailable") === "true";
    setBusy(true);
    setError("");
    try {
      setData(
        await api<Metadata>(`/products/${productId}/metadata`, body, "PUT"),
      );
      setDirty(false);
      await onChanged();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function review() {
    if (!data) return;
    setBusy(true);
    setError("");
    try {
      setData(
        await api<Metadata>(`/products/${productId}/metadata/review`, {
          expectedVersion: data.version,
        }),
      );
      await onChanged();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <section className="panel form-panel">
      <h2>Thông số đất · sản phẩm #{productId}</h2>
      <p>
        Nhập theo nhãn hoặc tài liệu của nhà cung cấp. Để trống nếu chưa biết.
        Lưu thay đổi sẽ hủy lần duyệt trước.
      </p>
      <Notice error={error} />
      {data && (
        <>
          <form
            key={data.version}
            onSubmit={save}
            onChange={() => setDirty(true)}
          >
            <div className="form-grid">
              {Object.entries(physical).map(([key, label]) => (
                <Field key={key} label={label}>
                  <select
                    name={key}
                    defaultValue={data[key as keyof typeof physical] ?? ""}
                  >
                    <Options values={levels} />
                  </select>
                </Field>
              ))}
              {[
                ["phMin", "pH thấp nhất", 14, "0.01"],
                ["phMax", "pH cao nhất", 14, "0.01"],
                ["ecMin", "EC thấp nhất (mS/cm)", 99999.999, "0.001"],
                ["ecMax", "EC cao nhất (mS/cm)", 99999.999, "0.001"],
              ].map(([key, label, max, step]) => (
                <Field key={key} label={String(label)}>
                  <input
                    name={String(key)}
                    type="number"
                    min="0"
                    max={max}
                    step={step}
                    defaultValue={data[key as "phMin"] ?? ""}
                  />
                </Field>
              ))}
              <Field label="Phương pháp đo EC">
                <select name="ecMethod" defaultValue={data.ecMethod ?? ""}>
                  <Options values={methods} />
                </select>
              </Field>
              <Field label="Vị trí trồng">
                <select name="useContext" defaultValue={data.useContext ?? ""}>
                  <Options values={contexts} />
                </select>
              </Field>
              <Field label="Có mẫu thử">
                <select
                  name="sampleAvailable"
                  defaultValue={
                    data.sampleAvailable === null
                      ? ""
                      : String(data.sampleAvailable)
                  }
                >
                  <option value="">Chưa có thông tin</option>
                  <option value="true">Có</option>
                  <option value="false">Không</option>
                </select>
              </Field>
              {Object.entries({
                ...notes,
                ingredients: "Thành phần",
                sourceReference: "Nguồn thông tin",
              }).map(([key, label]) => (
                <Field key={key} label={label}>
                  <textarea
                    name={key}
                    maxLength={
                      key === "sourceReference"
                        ? 2000
                        : key === "ingredients"
                          ? 5000
                          : 3000
                    }
                    defaultValue={data[key as keyof typeof notes] ?? ""}
                  />
                </Field>
              ))}
            </div>
            <fieldset>
              <legend>Nhóm cây theo thông tin nguồn</legend>
              {Object.entries(groups).map(([value, label]) => (
                <label className="soil-check" key={value}>
                  <input
                    type="checkbox"
                    name="plantGroups"
                    value={value}
                    defaultChecked={data.plantGroups.includes(value)}
                  />
                  {label}
                </label>
              ))}
            </fieldset>
            <div className="action-bar">
              <button disabled={busy}>Lưu thông số</button>
              <button
                type="button"
                className="secondary"
                disabled={
                  busy || dirty || !!data.reviewedAt || !data.sourceReference
                }
                onClick={review}
              >
                Duyệt thông tin theo nguồn
              </button>
              <button type="button" className="secondary" onClick={onClose}>
                Đóng
              </button>
            </div>
          </form>
          <p>
            Duyệt nguồn không phải chứng nhận kiểm nghiệm hoặc bảo đảm sạch mầm
            bệnh.
          </p>
          <details>
            <summary>Thông tin đã lưu</summary>
            <SoilFacts data={data} />
          </details>
        </>
      )}
    </section>
  );
}
