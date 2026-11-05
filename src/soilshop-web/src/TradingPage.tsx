import { useCallback, useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";
import { api, ApiError } from "./api";
import { Empty, Field, Notice, Pagination } from "./components";
import type { PageResult, Sku, Supplier, Trade, TradeQuote } from "./types";
import { money, time } from "./types";
type DraftLine = { key: string; skuId: string; quantity: number; cost: string };
const emptyLine = (): DraftLine => ({
  key: crypto.randomUUID(),
  skuId: "",
  quantity: 1,
  cost: "",
});
export function TradingPage({ receipt, isAdmin }: { receipt: boolean; isAdmin: boolean }) {
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
  const cancelKeys = useRef(new Map<number, string>());
  const [editing, setEditing] = useState<Trade>();
  const [supplierId, setSupplierId] = useState("");
  const [review, setReview] = useState<{ trade: Trade; quote: TradeQuote }>();
  const [cancelling, setCancelling] = useState<Trade>();
  const [reason, setReason] = useState("");
  const [deleting, setDeleting] = useState<Trade>();
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
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await api<Trade>(editing ? `${resource}/${editing.id}` : resource, {
        ...(editing ? { expectedVersion: editing.version } : {}),
        ...(receipt ? { supplierId: Number(supplierId) } : {}),
        lines: lines.map((line) => ({
          skuId: Number(line.skuId),
          quantity: line.quantity,
          ...(receipt
            ? { unitCostVnd: line.cost ? Number(line.cost) : null }
            : {}),
        })),
      }, editing ? "PUT" : "POST");
      setOpen(false);
      setEditing(undefined);
      setReview(undefined);
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
  function edit(trade: Trade) {
    setEditing(trade); setOpen(true); setError(""); setReview(undefined);
    setSupplierId(String(trade.supplierId ?? ""));
    setLines(trade.lines.map(line => ({ key: crypto.randomUUID(), skuId: String(line.skuId), quantity: line.quantity, cost: line.unitPriceVnd == null ? "" : String(line.unitPriceVnd) })));
    window.scrollTo({ top: 0, behavior: "smooth" });
  }
  async function quote(trade: Trade) {
    setBusy(true); setError(""); setReview(undefined);
    try {
      const current = await api<Trade>(`${resource}/${trade.id}`);
      const value = await api<TradeQuote>(`${resource}/${trade.id}/quote`);
      setReview({ trade: current, quote: value });
      window.scrollTo({ top: 0, behavior: "smooth" });
    } catch (e) { setError((e as Error).message); }
    finally { setBusy(false); }
  }
  async function confirm(trade: Trade) {
    if (!review || review.trade.id !== trade.id) return;
    setBusy(true);
    setError("");
    setNotice("");
    const actionKey = keys.current.get(trade.id) ?? crypto.randomUUID();
    keys.current.set(trade.id, actionKey);
    try {
      await api<Trade>(`${resource}/${trade.id}/confirm`, {
        actionKey,
        expectedVersion: review.quote.version,
        expectedTotalVnd: review.quote.totalVnd,
      });
      setReview(undefined); keys.current.delete(trade.id);
      setNotice("Confirmed. Stock and movement history have been updated.");
      await load();
    } catch (e) {
      setError((e as Error).message);
      if (e instanceof ApiError && e.status === 409) {
        keys.current.delete(trade.id);
        setReview(e.quote ? { trade, quote: e.quote } : undefined);
      }
      await load().catch(() => {});
    } finally {
      setBusy(false);
    }
  }
  async function removeDraft() {
    if (!deleting) return;
    setBusy(true); setError("");
    try {
      await api<void>(`${resource}/${deleting.id}?expectedVersion=${deleting.version}`, undefined, "DELETE");
      setDeleting(undefined); setReview(undefined); setNotice("Draft deleted. Stock is unchanged."); await load();
    } catch (e) { setError((e as Error).message); }
    finally { setBusy(false); }
  }
  async function cancel(event: FormEvent) {
    event.preventDefault(); if (!cancelling) return;
    setBusy(true); setError("");
    const key = cancelKeys.current.get(cancelling.id) ?? crypto.randomUUID();
    cancelKeys.current.set(cancelling.id, key);
    try {
      await api<Trade>(`${resource}/${cancelling.id}/cancel`, { actionKey: key, expectedVersion: cancelling.version, reason });
      cancelKeys.current.delete(cancelling.id); setCancelling(undefined); setReason("");
      setNotice("Cancelled in full. Original history is preserved; reverse stock movements recorded."); await load();
    } catch (e) {
      setError((e as Error).message);
      if (e instanceof ApiError && e.status === 409) cancelKeys.current.delete(cancelling.id);
    } finally { setBusy(false); }
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
          disabled={busy}
          onClick={() => {
            setEditing(undefined); setLines([emptyLine()]); setSupplierId("");
            setOpen(true);
            setError("");
          }}
        >
          + New {receipt ? "receipt" : "sale"}
        </button>
      </div>
      <Notice error={error} text={notice} />
      {open && (
        <section className="panel form-panel">
          <h2>{editing ? "Edit" : "New"} {receipt ? "receipt" : "sale"} draft</h2>
          <p className="muted">
            Select from the first 100 SKUs. Each SKU may appear once.
          </p>
          <form onSubmit={create}>
            {receipt && (
              <Field label="Supplier">
                <select name="supplierId" required value={supplierId} onChange={e => setSupplierId(e.target.value)}>
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
      {review && (
        <section className="panel form-panel" aria-label="Review quote">
          <h2>Review {review.trade.code.slice(0, 12).toUpperCase()}</h2>
          <ul className="document-lines">{review.quote.lines.map(line => (
            <li key={line.skuId}>{skus.find(s => s.id === line.skuId)?.code ?? `SKU #${line.skuId}`} · {line.quantity} bags × {money(line.unitPriceVnd)}</li>
          ))}</ul>
          <p><strong>Total: {money(review.quote.totalVnd)}</strong></p>
          <p className="muted">Review before confirming. A draft does not reserve stock. Changed prices require another review.</p>
          <div className="form-actions">
            <button disabled={busy} onClick={() => confirm(review.trade)}>Confirm {receipt ? "receipt" : "sale"}</button>
            <button disabled={busy} className="secondary" onClick={() => setReview(undefined)}>Close quote</button>
          </div>
        </section>
      )}
      {deleting && <section className="panel form-panel" aria-label="Delete draft">
        <h2>Delete draft {deleting.code.slice(0, 12).toUpperCase()}?</h2>
        <p>Only this draft will be removed. It has not changed stock.</p>
        <div className="form-actions"><button disabled={busy} onClick={removeDraft}>Delete draft</button><button className="secondary" disabled={busy} onClick={() => setDeleting(undefined)}>Keep draft</button></div>
      </section>}
      {cancelling && <section className="panel form-panel" aria-label="Cancel document">
        <h2>Cancel {cancelling.code.slice(0, 12).toUpperCase()} in full</h2>
        <p>{receipt ? "The original bag quantities will be deducted. All lines require sufficient current stock." : "The original bag quantities will be restored."} History and frozen prices remain.</p>
        <form onSubmit={cancel}>
          <Field label="Cancellation reason"><textarea required maxLength={500} value={reason} onChange={e => { setReason(e.target.value); cancelKeys.current.delete(cancelling.id); }} /></Field>
          <div className="form-actions"><button disabled={busy || !reason.trim()}>Confirm cancellation</button><button type="button" className="secondary" disabled={busy} onClick={() => setCancelling(undefined)}>Keep document</button></div>
        </form>
      </section>}
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
                      className={`badge ${trade.status === "Confirmed" ? "good" : "neutral"}`}
                    >
                      {trade.status}
                    </span>
                    {trade.confirmedAt && <small>Confirmed {time(trade.confirmedAt)} · User #{trade.confirmedBy}</small>}
                    {trade.cancelledAt && <small>Cancelled {time(trade.cancelledAt)} · User #{trade.cancelledBy}: {trade.cancelReason}</small>}
                  </td>
                  <td className="number">
                    {trade.totalVnd == null
                      ? "On confirmation"
                      : money(trade.totalVnd)}
                  </td>
                  <td>
                    {trade.status === "Draft" ? (
                      <div className="form-actions">
                        <button disabled={busy} onClick={() => quote(trade)}>Review total</button>
                        <button className="secondary" disabled={busy} onClick={() => edit(trade)}>Edit draft</button>
                        <button className="secondary" disabled={busy} onClick={() => setDeleting(trade)}>Delete</button>
                      </div>
                    ) : (
                      trade.status === "Confirmed" && isAdmin ? <button className="secondary" disabled={busy} onClick={() => { setCancelling(trade); setReason(""); }}>Cancel document</button> : <span className="muted">Recorded</span>
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
