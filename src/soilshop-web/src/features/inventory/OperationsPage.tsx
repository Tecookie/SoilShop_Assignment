import { useEffect, useState } from "react";
import { api } from "../../shared/api/client";
import { Empty, Field, Notice, Pagination } from "../../shared/components";
import type { Movement, Suggestion } from "./types";
import type { PageResult } from "../../shared/types/paging";
import { time } from "../../shared/lib/format";
import { SearchSelect } from "../../shared/components/SearchSelect";
export function OperationsPage({
  suggestions,
  onReceipt,
}: {
  suggestions: boolean;
  onReceipt: () => void;
}) {
  const [movements, setMovements] = useState<PageResult<Movement>>();
  const [suggestionData, setSuggestionData] =
    useState<PageResult<Suggestion>>();
  const [search, setSearch] = useState("");
  const rows = suggestionData?.items ?? [];
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [refresh, setRefresh] = useState(0);
  const [converting, setConverting] = useState<Suggestion>();
  const [supplier, setSupplier] = useState("");
  const [busy, setBusy] = useState(false);
  async function convert() {
    if (!converting) return;
    setBusy(true);
    setError("");
    try {
      await api(`/restock-suggestions/${converting.id}/convert`, {
        expectedVersion: converting.version,
        supplierId: Number(supplier),
      });
      setConverting(undefined);
      onReceipt();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError("");
    const load = async () => {
      if (suggestions) {
        const data = await api<PageResult<Suggestion>>(
          `/restock-suggestions/search?page=${page}&search=${encodeURIComponent(search)}`,
        );
        if (active) setSuggestionData(data);
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
  }, [suggestions, page, refresh, search]);
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
              ? "Low-stock suggestions are refreshed asynchronously from current stock."
              : "Every confirmed stock change records its resulting bag balance."}
          </p>
        </div>
        <button className="secondary" onClick={() => setRefresh((x) => x + 1)}>
          Refresh
        </button>
      </div>
      <Notice error={error} />
      {converting && (
        <section className="panel form-panel">
          <h2>Prepare receipt for SKU #{converting.skuId}</h2>
          <p className="muted">
            Creates a Draft only. Review quantities and actual cost in Purchase
            receipts before confirming.
          </p>
          <SearchSelect
            label="Supplier"
            resource="/supplier-directory"
            value={supplier}
            onChange={setSupplier}
          />
          <div className="form-actions">
            <button disabled={busy || !supplier} onClick={convert}>
              Create receipt draft
            </button>
            <button
              className="secondary"
              onClick={() => setConverting(undefined)}
            >
              Close
            </button>
          </div>
        </section>
      )}
      <section className="panel">
        {suggestions && (
          <Field label="Search suggestions by SKU or product">
            <input
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
            />
          </Field>
        )}
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
                  <th>Action</th>
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
                      <td>
                        {r.status === "Open" ? (
                          <button
                            className="secondary"
                            onClick={() => {
                              setConverting(r);
                              setSupplier(r.supplierId?.toString() ?? "");
                            }}
                          >
                            Prepare receipt
                          </button>
                        ) : (
                          <button className="secondary" onClick={onReceipt}>
                            Open receipts
                          </button>
                        )}
                      </td>
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
        {suggestions && suggestionData && (
          <Pagination
            page={page}
            total={suggestionData.total}
            onChange={setPage}
          />
        )}
      </section>
    </>
  );
}
