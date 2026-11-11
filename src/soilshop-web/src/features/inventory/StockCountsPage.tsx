import { useCallback, useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";
import { api } from "../../shared/api/client";
import { Empty, Field, Notice, Pagination } from "../../shared/components";
import { SearchSelect } from "../../shared/components/SearchSelect";
import type { PageResult } from "../../shared/types/paging";
import type { Sku } from "../catalogue/types";
import { time } from "../../shared/lib/format";
interface Count {
  id: number;
  skuId: number;
  expectedQty: number;
  actualQty: number;
  status: string;
  reason: string;
  version: number;
  reviewNote: string | null;
  countedAt: string;
}
export function StockCountsPage({ isAdmin }: { isAdmin: boolean }) {
  const [data, setData] = useState<PageResult<Count>>();
  const [page, setPage] = useState(1);
  const [skuId, setSkuId] = useState("");
  const [captured, setCaptured] = useState<Sku>();
  const [review, setReview] = useState<Count>();
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const adjustKey = useRef(crypto.randomUUID());
  const load = useCallback(
    () => api<PageResult<Count>>(`/stock-counts?page=${page}`).then(setData),
    [page],
  );
  useEffect(() => {
    load().catch((e) => setError(e.message));
  }, [load]);
  async function capture() {
    setBusy(true);
    setError("");
    try {
      setCaptured(await api<Sku>(`/skus/${skuId}`));
      adjustKey.current = crypto.randomUUID();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function submit(e: FormEvent<HTMLFormElement>, adjust = false) {
    e.preventDefault();
    if (!captured) return;
    const form = new FormData(e.currentTarget);
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await api(
        adjust ? "/stock-adjustments" : "/stock-counts",
        adjust
          ? {
              requestKey: adjustKey.current,
              skuId: captured.id,
              expectedStockVersion: captured.stockVersion,
              deltaQty: Number(form.get("delta")),
              reason: form.get("reason"),
            }
          : {
              skuId: captured.id,
              expectedQty: captured.stockQty,
              expectedStockVersion: captured.stockVersion,
              actualQty: Number(form.get("actual")),
              reason: form.get("reason"),
            },
      );
      setCaptured(undefined);
      adjustKey.current = crypto.randomUUID();
      setNotice(
        adjust
          ? "Adjustment recorded with stock history."
          : "Count submitted. Stock changes only after Admin approval.",
      );
      await load();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function decide(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!review) return;
    const f = new FormData(e.currentTarget);
    setBusy(true);
    setError("");
    try {
      const result = await api<Count>(`/stock-counts/${review.id}/review`, {
        expectedVersion: review.version,
        approve: f.get("decision") === "approve",
        note: f.get("note"),
      });
      setReview(undefined);
      setNotice(
        result.status === "RecountRequired"
          ? "Stock changed after counting. Capture a fresh stock count."
          : `Count ${result.status.toLowerCase()}.`,
      );
      await load();
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
          <p className="eyebrow">PHYSICAL INVENTORY</p>
          <h1>Stock counts</h1>
          <p className="muted">
            Capture system stock before counting. A changed stock version
            requires a recount.
          </p>
        </div>
        <button
          className="secondary"
          onClick={() => load().catch((e) => setError(e.message))}
        >
          Refresh
        </button>
      </div>
      <Notice error={error} text={notice} />
      <section className="panel form-panel">
        <h2>Capture a count</h2>
        <SearchSelect
          label="SKU"
          resource="/skus"
          value={skuId}
          activeOnly={false}
          onChange={(v) => {
            setSkuId(v);
            setCaptured(undefined);
          }}
        />
        <button disabled={!skuId || busy} onClick={capture}>
          Capture current stock
        </button>
        {captured && (
          <>
            <p>
              <strong>{captured.code}</strong> · Expected {captured.stockQty}{" "}
              bags · Stock version {captured.stockVersion}
            </p>
            <form onSubmit={(e) => submit(e)}>
              <div className="form-grid">
                <Field label="Actual bags">
                  <input
                    name="actual"
                    type="number"
                    min="0"
                    max="1000000"
                    required
                    defaultValue={captured.stockQty}
                  />
                </Field>
                <Field label="Count reason">
                  <input name="reason" maxLength={500} required />
                </Field>
              </div>
              <button disabled={busy}>Submit for approval</button>
            </form>
            {isAdmin && (
              <details>
                <summary>Direct Admin adjustment</summary>
                <p className="muted">
                  For known damage, loss or opening stock. Signed change; stock
                  cannot become negative.
                </p>
                <form
                  onSubmit={(e) => submit(e, true)}
                  onChange={() => {
                    adjustKey.current = crypto.randomUUID();
                  }}
                >
                  <div className="form-grid">
                    <Field label="Bag change">
                      <input
                        name="delta"
                        type="number"
                        min="-1000000"
                        max="1000000"
                        required
                      />
                    </Field>
                    <Field label="Adjustment reason">
                      <input name="reason" maxLength={500} required />
                    </Field>
                  </div>
                  <button disabled={busy}>Record adjustment</button>
                </form>
              </details>
            )}
          </>
        )}
      </section>
      {review && (
        <section className="panel form-panel">
          <h2>Review count #{review.id}</h2>
          <p>
            Expected {review.expectedQty}; counted {review.actualQty}.{" "}
            {review.reason}
          </p>
          <form onSubmit={decide}>
            <Field label="Decision">
              <select name="decision">
                <option value="approve">Approve if stock is unchanged</option>
                <option value="reject">Reject</option>
              </select>
            </Field>
            <Field label="Review note">
              <input name="note" required maxLength={500} />
            </Field>
            <div className="form-actions">
              <button disabled={busy}>Submit review</button>
              <button
                type="button"
                className="secondary"
                onClick={() => setReview(undefined)}
              >
                Close review
              </button>
            </div>
          </form>
        </section>
      )}
      <section className="panel">
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Count / SKU</th>
                <th>Expected</th>
                <th>Actual</th>
                <th>Reason / review</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((x) => (
                <tr key={x.id}>
                  <td>
                    #{x.id} / SKU #{x.skuId}
                    <small>{time(x.countedAt)}</small>
                  </td>
                  <td>{x.expectedQty}</td>
                  <td>{x.actualQty}</td>
                  <td>
                    {x.reason}
                    <small>{x.reviewNote}</small>
                  </td>
                  <td>{x.status}</td>
                  <td>
                    {isAdmin && x.status === "Pending" && (
                      <button
                        className="secondary"
                        disabled={busy}
                        onClick={() => setReview(x)}
                      >
                        Review
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!data?.items.length && (
            <Empty>{data ? "No counts yet." : "Loading counts…"}</Empty>
          )}
        </div>
        {data && (
          <Pagination page={page} total={data.total} onChange={setPage} />
        )}
      </section>
    </>
  );
}
