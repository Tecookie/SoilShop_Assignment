import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { api } from "../../shared/api/client";
import { Empty, Field, Notice, Pagination } from "../../shared/components";
import type { PageResult } from "../../shared/types/paging";
interface Supplier {
  id: number;
  code: string;
  name: string;
  contactNote: string | null;
  isActive: boolean;
  version: number;
}
export function SuppliersPage() {
  const [data, setData] = useState<PageResult<Supplier>>();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [editing, setEditing] = useState<Supplier | null>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const load = useCallback(
    () =>
      api<PageResult<Supplier>>(
        `/supplier-directory?page=${page}&search=${encodeURIComponent(search)}`,
      ).then(setData),
    [page, search],
  );
  useEffect(() => {
    load().catch((e) => setError(e.message));
  }, [load]);
  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setBusy(true);
    setError("");
    try {
      await api(
        editing ? `/supplier-directory/${editing.id}` : "/suppliers",
        {
          code: f.get("code"),
          name: f.get("name"),
          contactNote: f.get("note") || null,
          ...(editing
            ? {
                expectedVersion: editing.version,
                isActive: f.get("active") === "on",
              }
            : {}),
        },
        editing ? "PUT" : "POST",
      );
      setEditing(undefined);
      setNotice("Supplier saved. History is preserved when deactivated.");
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
          <h1>Suppliers</h1>
          <p className="muted">
            Maintain contact information. Deactivate suppliers instead of
            removing history.
          </p>
        </div>
        <button onClick={() => setEditing(null)}>New supplier</button>
      </div>
      <Notice error={error} text={notice} />
      {editing !== undefined && (
        <section className="panel form-panel">
          <h2>{editing ? "Edit supplier" : "Create supplier"}</h2>
          <form key={editing?.id ?? "new"} onSubmit={save}>
            <div className="form-grid">
              <Field label="Code">
                <input
                  name="code"
                  defaultValue={editing?.code}
                  maxLength={40}
                  required
                />
              </Field>
              <Field label="Name">
                <input
                  name="name"
                  defaultValue={editing?.name}
                  maxLength={200}
                  required
                />
              </Field>
              <Field label="Contact note">
                <input
                  name="note"
                  defaultValue={editing?.contactNote ?? ""}
                  maxLength={2000}
                />
              </Field>
              {editing && (
                <Field label="Active">
                  <input
                    name="active"
                    type="checkbox"
                    defaultChecked={editing.isActive}
                  />
                </Field>
              )}
            </div>
            <div className="form-actions">
              <button disabled={busy}>Save supplier</button>
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
        <Field label="Search suppliers">
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
                <th>Code</th>
                <th>Name</th>
                <th>Contact</th>
                <th>State</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((x) => (
                <tr key={x.id}>
                  <td>{x.code}</td>
                  <td>{x.name}</td>
                  <td>{x.contactNote ?? "—"}</td>
                  <td>{x.isActive ? "Active" : "Inactive"}</td>
                  <td>
                    <button className="secondary" onClick={() => setEditing(x)}>
                      Edit supplier
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!data?.items.length && (
            <Empty>
              {data ? "No matching suppliers." : "Loading suppliers…"}
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
