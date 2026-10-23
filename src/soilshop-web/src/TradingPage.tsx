import { useCallback, useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";
import { api } from "./api";
import { Empty, Field, Notice, Pagination } from "./components";
import type { PageResult, Sku, Supplier, Trade } from "./types";
import { money, time } from "./types";
type DraftLine = { key: string; skuId: string; quantity: number; cost: string };
const emptyLine = (): DraftLine => ({
  key: crypto.randomUUID(),
  skuId: "",
  quantity: 1,
  cost: "",
});
export function TradingPage({ receipt }: { receipt: boolean }) {
  const resource = receipt ? "/purchase-receipts" : "/sales";
  const [data, setData] = useState<PageResult<Trade>>();
  const [skus, setSkus] = useState<Sku[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [page, setPage] = useState(1);
  const [lines, setLines] = useState<DraftLine[]>([emptyLine()]);
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const keys = useRef(new Map<number, string>());
  const load = useCallback(async () => {
    const [trades, inventory, partners] = await Promise.all([
      api<PageResult<Trade>>(`${resource}?page=${page}`),
      api<PageResult<Sku>>("/skus?pageSize=100"),
      api<Supplier[]>("/suppliers"),
    ]);
    setData(trades);
    setSkus(inventory.items);
    setSuppliers(partners);
  }, [resource, page]);
  useEffect(() => {
    load().catch((e) => setError(e.message));
  }, [load]);
  function changeLine(key: string, value: Partial<DraftLine>) {
    setLines((lines) =>
      lines.map((line) => (line.key === key ? { ...line, ...value } : line)),
    );
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await api<Trade>(resource, {
        ...(receipt ? { supplierId: Number(form.get("supplierId")) } : {}),
        lines: lines.map((line) => ({
          skuId: Number(line.skuId),
          quantity: line.quantity,
          ...(receipt
            ? { unitCostVnd: line.cost ? Number(line.cost) : null }
            : {}),
        })),
      });
      setOpen(false);
      setLines([emptyLine()]);
      setNotice(
        "Draft saved. Review its lines below, then confirm to change stock.",
      );
      await load();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function confirm(trade: Trade) {
    setBusy(true);
    setError("");
    setNotice("");
    const actionKey = keys.current.get(trade.id) ?? crypto.randomUUID();
    keys.current.set(trade.id, actionKey);
    try {
      await api<Trade>(`${resource}/${trade.id}/confirm`, {
        actionKey,
        expectedVersion: trade.version,
      });
      setNotice("Confirmed. Stock and movement history have been updated.");
      await load();
    } catch (e) {
      setError((e as Error).message);
      await load().catch(() => {});
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">{receipt ? "GOODS IN" : "COUNTER SALES"}</p>
          <h1>{receipt ? "Purchase receipts" : "Sales"}</h1>
          <p className="muted">
            {receipt
              ? "Record sealed bags delivered by a supplier."
              : "Record a walk-in sale at current catalogue prices."}{" "}
            Stock changes only on confirmation.
          </p>
        </div>
        <button
          onClick={() => {
            setOpen((x) => !x);
            setError("");
          }}
        >
          + New {receipt ? "receipt" : "sale"}
        </button>
      </div>
      <Notice error={error} text={notice} />
      {open && (
        <section className="panel form-panel">
          <h2>New {receipt ? "receipt" : "sale"} draft</h2>
          <p className="muted">
            Select from the first 100 SKUs. Each SKU may appear once.
          </p>
          <form onSubmit={create}>
            {receipt && (
              <Field label="Supplier">
                <select name="supplierId" required defaultValue="">
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
                            (l) =>
                              l.key !== line.key && l.skuId === String(s.id),
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
                      required
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
                  onClick={() =>
                    setLines((lines) => lines.filter((l) => l.key !== line.key))
                  }
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
                onClick={() => setLines((lines) => [...lines, emptyLine()])}
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
                onClick={() => setOpen(false)}
              >
                Close form
              </button>
            </div>
          </form>
        </section>
      )}
      <section className="panel">
        <div className="section-heading">
          <h2>Documents</h2>
          <button
            className="secondary"
            onClick={() => load().catch((e) => setError(e.message))}
          >
            Refresh
          </button>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Reference / created</th>
                <th>Lines</th>
                <th>Status</th>
                <th className="number">Confirmed total</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((trade) => (
                <tr key={trade.id}>
                  <td>
                    <strong title={trade.code}>
                      {trade.code.slice(0, 12).toUpperCase()}
                    </strong>
                    <small>{time(trade.createdAt)}</small>
                  </td>
                  <td>
                    <ul className="document-lines">
                      {trade.lines.map((line) => (
                        <li key={line.skuId}>
                          <strong>{line.quantity} bags</strong> ·{" "}
                          {line.label ??
                            skus.find((s) => s.id === line.skuId)?.code ??
                            `SKU #${line.skuId}`}
                          {line.unitPriceVnd != null && (
                            <small>{money(line.unitPriceVnd)} / bag</small>
                          )}
                        </li>
                      ))}
                    </ul>
                  </td>
                  <td>
                    <span
                      className={`badge ${trade.status === "Draft" ? "neutral" : "good"}`}
                    >
                      {trade.status}
                    </span>
                  </td>
                  <td className="number">
                    {trade.totalVnd == null
                      ? "On confirmation"
                      : money(trade.totalVnd)}
                  </td>
                  <td>
                    {trade.status === "Draft" ? (
                      <button disabled={busy} onClick={() => confirm(trade)}>
                        Confirm {receipt ? "receipt" : "sale"}
                      </button>
                    ) : (
                      <span className="muted">Recorded</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!data?.items.length && (
            <Empty>
              {data
                ? `No ${receipt ? "receipts" : "sales"} yet. Create a draft to get started.`
                : "Loading documents…"}
            </Empty>
          )}
        </div>
        {data && (
          <Pagination page={page} total={data.total} onChange={setPage} />
        )}
      </section>
    </>
  );
}
