import { useCallback, useEffect, useState } from "react";
import { api } from "../../shared/api/client";
import { Empty, Field, Notice, Pagination } from "../../shared/components";
import type { PageResult } from "../../shared/types/paging";
import { money, time } from "../../shared/lib/format";
interface Summary {
  id: number;
  businessDate: string;
  status: string;
  revenueVnd: number;
  soldBags: number;
  saleCount: number;
  snapshotAt: string | null;
  lastError: string | null;
}
interface Detail {
  summary: Summary;
  stock: { id: number; skuId: number; stockQty: number }[];
}
export function ReportsPage() {
  const [data, setData] = useState<PageResult<Summary>>();
  const [detail, setDetail] = useState<Detail>();
  const [page, setPage] = useState(1);
  const [date, setDate] = useState(
    new Intl.DateTimeFormat("en-CA", {
      timeZone: "Asia/Ho_Chi_Minh",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
    }).format(new Date()),
  );
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const load = useCallback(
    () => api<PageResult<Summary>>(`/summaries?page=${page}`).then(setData),
    [page],
  );
  useEffect(() => {
    load().catch((e) => setError(e.message));
  }, [load]);
  async function refresh() {
    setBusy(true);
    setError("");
    try {
      await api(`/summaries/${date}/refresh`, {}, "POST");
      setNotice(
        "Refresh queued. Reload results after the Worker processes it.",
      );
      setDetail(undefined);
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
          <p className="eyebrow">VIETNAM BUSINESS DATE</p>
          <h1>Daily summaries</h1>
          <p className="muted">
            Effective confirmed sales for the selected day. Stock is captured
            when the job runs, not historical closing stock.
          </p>
        </div>
        <button
          className="secondary"
          onClick={() => load().catch((e) => setError(e.message))}
        >
          Reload results
        </button>
      </div>
      <Notice error={error} text={notice} />
      <section className="panel form-panel">
        <div className="form-grid">
          <Field label="Business date">
            <input
              type="date"
              value={date}
              min="2000-01-01"
              required
              onChange={(e) => setDate(e.target.value)}
            />
          </Field>
        </div>
        <button disabled={busy || !date} onClick={refresh}>
          Request summary refresh
        </button>
      </section>
      <section className="panel">
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Status</th>
                <th>Revenue</th>
                <th>Sales</th>
                <th>Bags sold</th>
                <th>Snapshot time</th>
                <th>Action / error</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((x) => (
                <tr key={x.id}>
                  <td>{x.businessDate}</td>
                  <td>{x.status}</td>
                  <td>{money(x.revenueVnd)}</td>
                  <td>{x.saleCount}</td>
                  <td>{x.soldBags}</td>
                  <td>{x.snapshotAt ? time(x.snapshotAt) : "Not generated"}</td>
                  <td>
                    {x.lastError}
                    <button
                      className="secondary"
                      onClick={() =>
                        api<Detail>(`/summaries/${x.businessDate}`)
                          .then(setDetail)
                          .catch((e) => setError(e.message))
                      }
                    >
                      View stock snapshot
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!data?.items.length && (
            <Empty>{data ? "No summaries yet." : "Loading summaries…"}</Empty>
          )}
        </div>
        {data && (
          <Pagination page={page} total={data.total} onChange={setPage} />
        )}
      </section>
      {detail && (
        <section className="panel">
          <h2>Stock captured for {detail.summary.businessDate}</h2>
          <p className="muted">
            Captured{" "}
            {detail.summary.snapshotAt
              ? time(detail.summary.snapshotAt)
              : "not yet"}
            ; these values are a stored snapshot.
          </p>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>SKU</th>
                  <th>Bags at snapshot</th>
                </tr>
              </thead>
              <tbody>
                {detail.stock.map((x) => (
                  <tr key={x.id}>
                    <td>#{x.skuId}</td>
                    <td>{x.stockQty}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </>
  );
}
