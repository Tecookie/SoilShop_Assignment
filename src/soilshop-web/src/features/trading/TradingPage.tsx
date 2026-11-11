import { TradeDraftForm } from "./components/TradeDraftForm";
import { TradeReviewPanel } from "./components/TradeReviewPanel";
import { TradeDeletePanel } from "./components/TradeDeletePanel";
import { TradeCancellationPanel } from "./components/TradeCancellationPanel";
import { emptyLine } from "./draft";
import type { DraftLine } from "./draft";
import { useCallback, useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";
import { api, ApiError } from "../../shared/api/client";
import { Empty, Field, Notice, Pagination } from "../../shared/components";
import type { PageResult } from "../../shared/types/paging";
import type { Sku, Supplier } from "../catalogue/types";
import type { Trade, TradeQuote } from "./types";
import { money, time } from "../../shared/lib/format";
export function TradingPage({
  receipt,
  isAdmin,
}: {
  receipt: boolean;
  isAdmin: boolean;
}) {
  const resource = receipt ? "/purchase-receipts" : "/sales";
  const [data, setData] = useState<PageResult<Trade>>();
  const [skus, setSkus] = useState<Sku[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState({
    search: "",
    status: "",
    from: "",
    to: "",
    createdBy: "",
    supplierId: "",
  });
  const query = new URLSearchParams(
    Object.entries(filters).filter(([, v]) => v),
  );
  const filterQuery = query.toString();
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
      api<PageResult<Trade>>(`${resource}?page=${page}&${filterQuery}`),
      api<PageResult<Sku>>("/skus?pageSize=100"),
      api<Supplier[]>("/suppliers"),
    ]);
    setData(trades);
    setSkus(inventory.items);
    setSuppliers(partners);
  }, [resource, page, filterQuery]);
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
      await api<Trade>(
        editing ? `${resource}/${editing.id}` : resource,
        {
          ...(editing ? { expectedVersion: editing.version } : {}),
          ...(receipt ? { supplierId: Number(supplierId) } : {}),
          lines: lines.map((line) => ({
            skuId: Number(line.skuId),
            quantity: line.quantity,
            ...(receipt
              ? { unitCostVnd: line.cost ? Number(line.cost) : null }
              : {}),
          })),
        },
        editing ? "PUT" : "POST",
      );
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
    setEditing(trade);
    setOpen(true);
    setError("");
    setReview(undefined);
    setSupplierId(String(trade.supplierId ?? ""));
    setLines(
      trade.lines.map((line) => ({
        key: crypto.randomUUID(),
        skuId: String(line.skuId),
        quantity: line.quantity,
        cost: line.unitPriceVnd == null ? "" : String(line.unitPriceVnd),
      })),
    );
    window.scrollTo({ top: 0, behavior: "smooth" });
  }
  async function quote(trade: Trade) {
    setBusy(true);
    setError("");
    setReview(undefined);
    try {
      const current = await api<Trade>(`${resource}/${trade.id}`);
      const value = await api<TradeQuote>(`${resource}/${trade.id}/quote`);
      setReview({ trade: current, quote: value });
      window.scrollTo({ top: 0, behavior: "smooth" });
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
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
      setReview(undefined);
      keys.current.delete(trade.id);
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
    setBusy(true);
    setError("");
    try {
      await api<void>(
        `${resource}/${deleting.id}?expectedVersion=${deleting.version}`,
        undefined,
        "DELETE",
      );
      setDeleting(undefined);
      setReview(undefined);
      setNotice("Draft deleted. Stock is unchanged.");
      await load();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function cancel(event: FormEvent) {
    event.preventDefault();
    if (!cancelling) return;
    setBusy(true);
    setError("");
    const key = cancelKeys.current.get(cancelling.id) ?? crypto.randomUUID();
    cancelKeys.current.set(cancelling.id, key);
    try {
      await api<Trade>(`${resource}/${cancelling.id}/cancel`, {
        actionKey: key,
        expectedVersion: cancelling.version,
        reason,
      });
      cancelKeys.current.delete(cancelling.id);
      setCancelling(undefined);
      setReason("");
      setNotice(
        "Cancelled in full. Original history is preserved; reverse stock movements recorded.",
      );
      await load();
    } catch (e) {
      setError((e as Error).message);
      if (e instanceof ApiError && e.status === 409)
        cancelKeys.current.delete(cancelling.id);
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
          disabled={busy}
          onClick={() => {
            setEditing(undefined);
            setLines([emptyLine()]);
            setSupplierId("");
            setOpen(true);
            setError("");
          }}
        >
          + New {receipt ? "receipt" : "sale"}
        </button>
      </div>
      <Notice error={error} text={notice} />
      <section className="panel">
        <div className="form-grid">
          <Field label="Search document code">
            <input
              value={filters.search}
              onChange={(e) => {
                setFilters((f) => ({ ...f, search: e.target.value }));
                setPage(1);
              }}
            />
          </Field>
          <Field label="Document status">
            <select
              value={filters.status}
              onChange={(e) => {
                setFilters((f) => ({ ...f, status: e.target.value }));
                setPage(1);
              }}
            >
              <option value="">All states</option>
              <option>Draft</option>
              <option>Confirmed</option>
              <option>Cancelled</option>
            </select>
          </Field>
          <Field label="Created from (Vietnam)">
            <input
              type="date"
              value={filters.from}
              onChange={(e) => {
                setFilters((f) => ({ ...f, from: e.target.value }));
                setPage(1);
              }}
            />
          </Field>
          <Field label="Created through (Vietnam)">
            <input
              type="date"
              value={filters.to}
              onChange={(e) => {
                setFilters((f) => ({ ...f, to: e.target.value }));
                setPage(1);
              }}
            />
          </Field>
          <Field label="Created by account ID">
            <input
              type="number"
              min="1"
              value={filters.createdBy}
              onChange={(e) => {
                setFilters((f) => ({ ...f, createdBy: e.target.value }));
                setPage(1);
              }}
            />
          </Field>
          {receipt && (
            <Field label="Supplier ID">
              <input
                type="number"
                min="1"
                value={filters.supplierId}
                onChange={(e) => {
                  setFilters((f) => ({ ...f, supplierId: e.target.value }));
                  setPage(1);
                }}
              />
            </Field>
          )}
        </div>
      </section>
      {open && (
        <TradeDraftForm
          receipt={receipt}
          editing={!!editing}
          supplierId={supplierId}
          onSupplierChange={setSupplierId}
          suppliers={suppliers}
          lines={lines}
          skus={skus}
          changeLine={changeLine}
          onRemoveLine={(key) =>
            setLines((lines) => lines.filter((l) => l.key !== key))
          }
          onAddLine={() => setLines((lines) => [...lines, emptyLine()])}
          busy={busy}
          create={create}
          onClose={() => setOpen(false)}
        />
      )}
      {review && (
        <TradeReviewPanel
          receipt={receipt}
          review={review}
          skus={skus}
          busy={busy}
          confirm={confirm}
          onClose={() => setReview(undefined)}
        />
      )}
      {deleting && (
        <TradeDeletePanel
          deleting={deleting}
          busy={busy}
          removeDraft={removeDraft}
          onClose={() => setDeleting(undefined)}
        />
      )}
      {cancelling && (
        <TradeCancellationPanel
          receipt={receipt}
          cancelling={cancelling}
          reason={reason}
          busy={busy}
          onReasonChange={(v) => {
            setReason(v);
            cancelKeys.current.delete(cancelling.id);
          }}
          cancel={cancel}
          onClose={() => setCancelling(undefined)}
        />
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
                            line.skuCode ??
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
                    {trade.confirmedAt && (
                      <small>
                        Confirmed {time(trade.confirmedAt)} · User #
                        {trade.confirmedBy}
                      </small>
                    )}
                    {trade.cancelledAt && (
                      <small>
                        Cancelled {time(trade.cancelledAt)} · User #
                        {trade.cancelledBy}: {trade.cancelReason}
                      </small>
                    )}
                  </td>
                  <td className="number">
                    {trade.totalVnd == null
                      ? "On confirmation"
                      : money(trade.totalVnd)}
                  </td>
                  <td>
                    {trade.status === "Draft" ? (
                      <div className="form-actions">
                        <button disabled={busy} onClick={() => quote(trade)}>
                          Review total
                        </button>
                        <button
                          className="secondary"
                          disabled={busy}
                          onClick={() => edit(trade)}
                        >
                          Edit draft
                        </button>
                        <button
                          className="secondary"
                          disabled={busy}
                          onClick={() => setDeleting(trade)}
                        >
                          Delete
                        </button>
                      </div>
                    ) : trade.status === "Confirmed" && isAdmin ? (
                      <button
                        className="secondary"
                        disabled={busy}
                        onClick={() => {
                          setCancelling(trade);
                          setReason("");
                        }}
                      >
                        Cancel document
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
