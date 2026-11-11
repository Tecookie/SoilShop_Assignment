import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { api } from "../../shared/api/client";
import { Empty, Field, Notice, Pagination } from "../../shared/components";
import type { PageResult } from "../../shared/types/paging";
interface Account {
  id: number;
  username: string;
  displayName: string;
  role: string;
  isActive: boolean;
  version: number;
}
export function AccountsPage() {
  const [data, setData] = useState<PageResult<Account>>();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [editing, setEditing] = useState<Account | null>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const load = useCallback(
    () =>
      api<PageResult<Account>>(
        `/accounts?page=${page}&search=${encodeURIComponent(search)}`,
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
        editing ? `/accounts/${editing.id}` : "/accounts",
        {
          username: f.get("username"),
          displayName: f.get("displayName"),
          role: f.get("role"),
          isActive: f.get("active") === "on",
          password: f.get("password") || null,
          expectedVersion: editing?.version ?? null,
        },
        editing ? "PUT" : "POST",
      );
      setEditing(undefined);
      setNotice(
        "Account saved. Existing sessions are invalidated when the account changes.",
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
          <p className="eyebrow">ADMINISTRATION</p>
          <h1>Staff accounts</h1>
          <p className="muted">
            Keep at least one active Admin. Account changes invalidate existing
            tokens.
          </p>
        </div>
        <button onClick={() => setEditing(null)}>New account</button>
      </div>
      <Notice error={error} text={notice} />
      {editing !== undefined && (
        <section className="panel form-panel">
          <h2>{editing ? "Edit account" : "Create account"}</h2>
          <form key={editing?.id ?? "new"} onSubmit={save}>
            <div className="form-grid">
              <Field label="Username">
                <input
                  name="username"
                  defaultValue={editing?.username}
                  pattern="[A-Za-z0-9._\-]+"
                  maxLength={100}
                  required
                />
              </Field>
              <Field label="Display name">
                <input
                  name="displayName"
                  defaultValue={editing?.displayName}
                  maxLength={150}
                  required
                />
              </Field>
              <Field label="Role">
                <select name="role" defaultValue={editing?.role ?? "Employee"}>
                  <option>Employee</option>
                  <option>Admin</option>
                </select>
              </Field>
              <Field
                label={
                  editing
                    ? "New password (leave blank to keep)"
                    : "Initial password"
                }
              >
                <input
                  name="password"
                  type="password"
                  autoComplete="new-password"
                  minLength={12}
                  maxLength={200}
                  required={!editing}
                />
              </Field>
              <Field label="Active">
                <input
                  name="active"
                  type="checkbox"
                  defaultChecked={editing?.isActive ?? true}
                />
              </Field>
            </div>
            <div className="form-actions">
              <button disabled={busy}>Save account</button>
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
        <Field label="Search accounts">
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
                <th>Username</th>
                <th>Name</th>
                <th>Role</th>
                <th>State</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {data?.items.map((x) => (
                <tr key={x.id}>
                  <td>{x.username}</td>
                  <td>{x.displayName}</td>
                  <td>{x.role}</td>
                  <td>{x.isActive ? "Active" : "Inactive"}</td>
                  <td>
                    <button className="secondary" onClick={() => setEditing(x)}>
                      Edit account
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!data?.items.length && (
            <Empty>
              {data ? "No matching accounts." : "Loading accounts…"}
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
