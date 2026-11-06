import { useEffect, useState } from "react";
import { api } from "../../shared/api/client";
import { Empty, Notice, Pagination } from "../../shared/components";
import type { Movement, Suggestion } from "./types";
import type { PageResult } from "../../shared/types/paging";
import { time } from "../../shared/lib/format";
export function OperationsPage({ suggestions }: { suggestions: boolean }) {
  const [movements, setMovements] = useState<PageResult<Movement>>();
  const [rows, setRows] = useState<Suggestion[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [refresh, setRefresh] = useState(0);
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError("");
    const load = async () => {
      if (suggestions) {
        const data = await api<Suggestion[]>("/restock-suggestions");
        if (active) setRows(data);
      } else {
        const data = await api<PageResult<Movement>>(
          `/stock-movements?page=${page}`,
        );
        if (active) setMovements(data);
      }
    };
    load()
      .catch((e) => {
        if (active) setError(e.message);
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [suggestions, page, refresh]);
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">
            {suggestions ? "WORKER INBOX" : "AUDIT TRAIL"}
          </p>
          <h1>{suggestions ? "Replenishment" : "Stock movements"}</h1>
          <p className="muted">
            {suggestions
              ? "Low-stock suggestions are refreshed asynchronously from current stock. First 100 active suggestions."
              : "Every confirmed stock change records its resulting bag balance."}
          </p>
        </div>
        <button className="secondary" onClick={() => setRefresh((x) => x + 1)}>
          Refresh
        </button>
      </div>
      <Notice error={error} />
      <section className="panel">
        <div className="table-wrap" aria-busy={loading}>
          {suggestions ? (
            <table>
              <thead>
                <tr>
                  <th>SKU</th>
                  <th className="number">Observed bags</th>
                  <th className="number">Suggested bags</th>
                  <th>Supplier</th>
                  <th>State</th>
                  <th>Updated</th>
                </tr>
              </thead>
              <tbody>
                {!loading &&
                  rows.map((r) => (
                    <tr key={r.id}>
                      <td>SKU #{r.skuId}</td>
                      <td className="number">{r.observedQty}</td>
                      <td className="number stock">{r.suggestedQty}</td>
                      <td>
                        {r.supplierId
                          ? `Supplier #${r.supplierId}`
                          : "Not assigned"}
                      </td>
                      <td>
                        <span className="badge warning">{r.status}</span>
                      </td>
                      <td>{time(r.updatedAt)}</td>
                    </tr>
                  ))}
              </tbody>
            </table>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Movement</th>
                  <th>SKU</th>
                  <th>Kind</th>
                  <th className="number">Bag change</th>
                  <th className="number">Balance after</th>
                  <th>Recorded</th>
                </tr>
              </thead>
              <tbody>
                {!loading &&
                  movements?.items.map((m) => (
                    <tr key={m.id}>
                      <td>#{m.id}</td>
                      <td>SKU #{m.skuId}</td>
                      <td>{m.kind}</td>
                      <td className="number">
                        {m.deltaQty > 0 ? "+" : ""}
                        {m.deltaQty}
                      </td>
                      <td className="number stock">{m.balanceAfter}</td>
                      <td>{time(m.createdAt)}</td>
                    </tr>
                  ))}
              </tbody>
            </table>
          )}
          {loading ? (
            <Empty>Loading records…</Empty>
          ) : (
            (suggestions ? !rows.length : !movements?.items.length) && (
              <Empty>
                {suggestions
                  ? "No active suggestions. The Worker checks low stock after changes and periodically."
                  : "No stock movements yet. Confirm a receipt or sale to record one."}
              </Empty>
            )
          )}
        </div>
        {!suggestions && movements && (
          <Pagination page={page} total={movements.total} onChange={setPage} />
        )}
      </section>
    </>
  );
}
