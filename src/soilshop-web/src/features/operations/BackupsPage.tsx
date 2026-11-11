import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "../../shared/api/client";
import { Empty, Notice, Pagination } from "../../shared/components";
import type { PageResult } from "../../shared/types/paging";
import { time } from "../../shared/lib/format";
interface Backup {
  id: string;
  kind: string;
  status: string;
  filename: string | null;
  retained: boolean;
  attempts: number;
  createdAt: string;
  completedAt: string | null;
  lastError: string | null;
}
export function BackupsPage() {
  const [data, setData] = useState<PageResult<Backup>>();
  const [page, setPage] = useState(1);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const key = useRef(crypto.randomUUID());
  const load = useCallback(
    () => api<PageResult<Backup>>(`/backups?page=${page}`).then(setData),
    [page],
  );
  useEffect(() => {
    load().catch((e) => setError(e.message));
  }, [load]);
  async function request() {
    setBusy(true);
    setError("");
    try {
      await api("/backups", { requestKey: key.current });
      key.current = crypto.randomUUID();
      setNotice("Backup queued. Refresh to see the verified archive result.");
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
          <p className="eyebrow">ADMIN OPERATIONS</p>
          <h1>Database backups</h1>
          <p className="muted">
            Daily at 00:15 Vietnam time and manual requests. Keep the latest
            seven successful archives across both triggers.
          </p>
        </div>
        <button
          disabled={
            busy ||
            data?.items.some(
              (x) => x.status === "Queued" || x.status === "Processing",
            )
          }
          onClick={request}
        >
          Request backup
        </button>
      </div>
      <Notice error={error} text={notice} />
      <section className="panel">
        <div className="section-heading">
          <h2>Backup runs</h2>
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
                <th>Requested</th>
                <th>Kind</th>
                <th>Status</th>
                <th>Archive</th>
                <th>Retention</th>
                <th>Completed / error</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((x) => (
                <tr key={x.id}>
                  <td>{time(x.createdAt)}</td>
                  <td>{x.kind}</td>
                  <td>
                    {x.status} · attempt {x.attempts}
                  </td>
                  <td>{x.filename ?? "Not available"}</td>
                  <td>
                    {x.retained
                      ? "Retained"
                      : x.status === "Completed"
                        ? "Pruned"
                        : "—"}
                  </td>
                  <td>
                    {x.completedAt ? time(x.completedAt) : "Pending"}
                    <small>{x.lastError}</small>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!data?.items.length && (
            <Empty>{data ? "No backup runs yet." : "Loading backups…"}</Empty>
          )}
        </div>
        {data && (
          <Pagination page={page} total={data.total} onChange={setPage} />
        )}
      </section>
      <p className="muted">
        Restore is an operator procedure into an isolated database. The app
        never overwrites the shop database.
      </p>
    </>
  );
}
