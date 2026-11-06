import type { FormEvent } from "react";
import type { Sku, Supplier } from "../../catalogue/types";
import type { DraftLine } from "../draft";
import { Field } from "../../../shared/components";
import { money } from "../../../shared/lib/format";
export function TradeDraftForm({
  receipt,
  editing,
  supplierId,
  onSupplierChange,
  suppliers,
  lines,
  skus,
  changeLine,
  onRemoveLine,
  onAddLine,
  busy,
  create,
  onClose,
}: {
  receipt: boolean;
  editing: boolean;
  supplierId: string;
  onSupplierChange: (v: string) => void;
  suppliers: Supplier[];
  lines: DraftLine[];
  skus: Sku[];
  changeLine: (key: string, v: Partial<DraftLine>) => void;
  onRemoveLine: (key: string) => void;
  onAddLine: () => void;
  busy: boolean;
  create: (e: FormEvent<HTMLFormElement>) => void;
  onClose: () => void;
}) {
  return (
    <section className="panel form-panel">
      <h2>
        {editing ? "Edit" : "New"} {receipt ? "receipt" : "sale"} draft
      </h2>
      <p className="muted">
        Select from the first 100 SKUs. Each SKU may appear once.
      </p>
      <form onSubmit={create}>
        {receipt && (
          <Field label="Supplier">
            <select
              name="supplierId"
              required
              value={supplierId}
              onChange={(e) => onSupplierChange(e.target.value)}
            >
              <option value="" disabled>
                Select supplier
              </option>
              {suppliers
                .filter((s) => s.isActive)
                .map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
            </select>
          </Field>
        )}
        {lines.map((line, index) => (
          <div className="line-editor" key={line.key}>
            <Field label={`SKU ${index + 1}`}>
              <select
                required
                value={line.skuId}
                onChange={(e) =>
                  changeLine(line.key, { skuId: e.target.value })
                }
              >
                <option value="" disabled>
                  Select packaging
                </option>
                {skus
                  .filter(
                    (s) =>
                      s.isActive &&
                      !lines.some(
                        (l) => l.key !== line.key && l.skuId === String(s.id),
                      ),
                  )
                  .map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.code} · {s.packageAmount} {s.packageUnit} ·{" "}
                      {s.stockQty} bags · {money(s.salePriceVnd)}
                    </option>
                  ))}
              </select>
            </Field>
            <Field label={`Bags ${index + 1}`}>
              <input
                type="number"
                min="1"
                max="1000000"
                step="1"
                required
                value={line.quantity}
                onChange={(e) =>
                  changeLine(line.key, { quantity: Number(e.target.value) })
                }
              />
            </Field>
            {receipt && (
              <Field label={`Unit cost ${index + 1} (VND)`}>
                <input
                  type="number"
                  min="1"
                  max="1000000000000"
                  step="1"
                  value={line.cost}
                  onChange={(e) =>
                    changeLine(line.key, { cost: e.target.value })
                  }
                />
              </Field>
            )}
            <button
              type="button"
              className="secondary"
              aria-label={`Remove line ${index + 1}`}
              disabled={lines.length === 1}
              onClick={() => onRemoveLine(line.key)}
            >
              Remove
            </button>
          </div>
        ))}
        <div className="form-actions">
          <button
            type="button"
            className="secondary"
            disabled={lines.length >= 100 || busy}
            onClick={() => onAddLine()}
          >
            + Add line
          </button>
          <button disabled={busy} type="submit">
            {busy ? "Saving…" : "Save draft"}
          </button>
          <button
            disabled={busy}
            className="secondary"
            type="button"
            onClick={() => onClose()}
          >
            Close form
          </button>
        </div>
      </form>
    </section>
  );
}
