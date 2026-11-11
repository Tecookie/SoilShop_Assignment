import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { api } from "../../shared/api/client";
import { Empty, Field, Notice, Pagination } from "../../shared/components";
import { SearchSelect } from "../../shared/components/SearchSelect";
import type { PageResult } from "../../shared/types/paging";
import type { Sku } from "../catalogue/types";
import { money } from "../../shared/lib/format";
export function SkuAdminPage() {
  const [data, setData] = useState<PageResult<Sku>>();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [editing, setEditing] = useState<Sku>();
  const [supplierId, setSupplierId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const load = useCallback(
    () =>
      api<PageResult<Sku>>(
        `/skus?page=${page}&search=${encodeURIComponent(search)}`,
      ).then(setData),
    [page, search],
  );
  useEffect(() => {
    load().catch((e) => setError(e.message));
  }, [load]);
  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!editing) return;
    const f = new FormData(e.currentTarget);
    setBusy(true);
    setError("");
    try {
      await api(
        `/skus/${editing.id}`,
        {
          productId: editing.productId,
          code: editing.code,
          packageAmount: editing.packageAmount,
          packageUnit: editing.packageUnit,
          salePriceVnd: Number(f.get("price")),
          lowStockThreshold: Number(f.get("threshold")),
          targetStock: Number(f.get("target")),
          preferredSupplierId: supplierId ? Number(supplierId) : null,
          expectedVersion: editing.version,
          isActive: f.get("active") === "on",
        },
        "PUT",
      );
      setEditing(undefined);
      setNotice("SKU saved. Historical sale prices are unchanged.");
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
          <p className="eyebrow">ADMINISTRATION</p>
          <h1>SKU prices & stock rules</h1>
          <p className="muted">
            Packaging identity is fixed. Change price, availability and
            replenishment rules here.
          </p>
        </div>
      </div>
      <Notice error={error} text={notice} />
      {editing && (
        <section className="panel form-panel">
          <h2>Edit {editing.code}</h2>
          <form key={editing.id} onSubmit={save}>
            <div className="form-grid">
              <Field label="Sale price (whole VND)">
                <input
                  name="price"
                  type="number"
                  min="1"
                  max="1000000000000"
                  step="1"
                  defaultValue={editing.salePriceVnd}
                  required
                />
              </Field>
              <Field label="Low stock threshold">
                <input
                  name="threshold"
                  type="number"
                  min="0"
                  max="1000000"
                  defaultValue={editing.lowStockThreshold}
                  required
                />
              </Field>
              <Field label="Target stock">
                <input
                  name="target"
                  type="number"
                  min="1"
                  max="1000000"
                  defaultValue={editing.targetStock}
                  required
                />
              </Field>
              <Field label="Active">
                <input
                  name="active"
                  type="checkbox"
                  defaultChecked={editing.isActive}
                />
              </Field>
            </div>
            <SearchSelect
              label="Preferred supplier"
              resource="/supplier-directory"
              value={supplierId}
              onChange={setSupplierId}
              required={false}
            />
            <div className="form-actions">
              <button disabled={busy}>Save SKU</button>
              <button
                type="button"
                className="secondary"
                onClick={() => setEditing(undefined)}
              >
                Close form
              </button>
            </div>
          </form>
        </section>
      )}
      <section className="panel">
        <Field label="Search SKUs">
          <input
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
        </Field>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>SKU</th>
                <th>Price</th>
                <th>Stock / threshold / target</th>
                <th>State</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((x) => (
                <tr key={x.id}>
                  <td>
                    {x.code}
                    <small>
                      {x.packageAmount} {x.packageUnit}
                    </small>
                  </td>
                  <td>{money(x.salePriceVnd)}</td>
                  <td>
                    {x.stockQty} / {x.lowStockThreshold} / {x.targetStock}
                  </td>
                  <td>{x.isActive ? "Active" : "Inactive"}</td>
                  <td>
                    <button
                      className="secondary"
                      onClick={() => {
                        setEditing(x);
                        setSupplierId(x.preferredSupplierId?.toString() ?? "");
                      }}
                    >
                      Edit SKU
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!data?.items.length && (
            <Empty>{data ? "No matching SKUs." : "Loading SKUs…"}</Empty>
          )}
        </div>
        {data && (
          <Pagination page={page} total={data.total} onChange={setPage} />
        )}
      </section>
    </>
  );
}
