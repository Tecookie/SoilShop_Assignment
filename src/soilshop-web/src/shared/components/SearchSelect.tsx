import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { PageResult } from "../types/paging";
import { Field } from "./Field";
type Choice = { id: number; code?: string; name?: string; isActive?: boolean };
export function SearchSelect({
  label,
  resource,
  value,
  onChange,
  required = true,
  activeOnly = true,
}: {
  label: string;
  resource: string;
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  activeOnly?: boolean;
}) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PageResult<Choice>>();
  const [labels, setLabels] = useState<Record<string, string>>({});
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  useEffect(() => {
    let alive = true;
    const timer = setTimeout(() => {
      setLoading(true);
      setError("");
      api<PageResult<Choice>>(
        `${resource}?search=${encodeURIComponent(search)}&page=${page}&pageSize=20${resource === "/supplier-directory" && activeOnly ? "&active=true" : ""}`,
      )
        .then((result) => {
          if (alive) {
            setData(result);
            setLabels((old) => ({
              ...old,
              ...Object.fromEntries(
                result.items.map((x) => [
                  String(x.id),
                  [x.code, x.name].filter(Boolean).join(" · "),
                ]),
              ),
            }));
          }
        })
        .catch((e) => {
          if (alive) setError(e.message);
        })
        .finally(() => {
          if (alive) setLoading(false);
        });
    }, 250);
    return () => {
      alive = false;
      clearTimeout(timer);
    };
  }, [resource, search, page, activeOnly]);
  const options =
    data?.items.filter((x) => !activeOnly || x.isActive !== false) ?? [];
  return (
    <div className="search-select">
      <Field label={`Search ${label}`}>
        <input
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          placeholder="Code or name"
        />
      </Field>
      <Field label={label}>
        <select
          required={required}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          aria-busy={loading}
        >
          <option value="">Select {label.toLowerCase()}</option>
          {value && !options.some((x) => String(x.id) === value) && (
            <option value={value}>
              {labels[value] ?? `Selected #${value}`}
            </option>
          )}
          {options.map((x) => (
            <option key={x.id} value={x.id}>
              {labels[String(x.id)] ?? x.code ?? x.name}
            </option>
          ))}
        </select>
      </Field>
      {loading && <small role="status">Searching…</small>}
      {error && <p role="alert">{error}</p>}
      <div className="form-actions">
        <button
          type="button"
          className="secondary"
          disabled={loading || page === 1}
          onClick={() => setPage((p) => p - 1)}
        >
          Previous choices
        </button>
        <span>
          Page {page} · {data?.total ?? 0} matches
        </span>
        <button
          type="button"
          className="secondary"
          disabled={loading || !data || page * 20 >= data.total}
          onClick={() => setPage((p) => p + 1)}
        >
          More choices
        </button>
      </div>
    </div>
  );
}
