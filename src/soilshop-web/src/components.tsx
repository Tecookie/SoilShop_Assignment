import { cloneElement, useId } from "react";
import type { ReactElement, ReactNode } from "react";
export function Field({
  label,
  children,
  hint,
}: {
  label: string;
  children: ReactElement<{ id?: string; "aria-describedby"?: string }>;
  hint?: string;
}) {
  const id = useId();
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      {cloneElement(children, { id, "aria-describedby": hint ? `${id}-hint` : undefined })}
      {hint && <small id={`${id}-hint`}>{hint}</small>}
    </div>
  );
}
export function Notice({ error, text }: { error?: string; text?: string }) {
  return (
    <>
      {error && (
        <div className="notice error" role="alert">
          {error}
        </div>
      )}
      {text && (
        <div className="notice success" role="status">
          {text}
        </div>
      )}
    </>
  );
}
export function Empty({ children }: { children: ReactNode }) {
  return (
    <div className="empty" role="status">
      {children}
    </div>
  );
}
export function Pagination({
  page,
  total,
  onChange,
}: {
  page: number;
  total: number;
  onChange: (page: number) => void;
}) {
  return (
    <div className="pagination">
      <span>
        {total} records · page {page} of {Math.max(1, Math.ceil(total / 20))}
      </span>
      <div>
        <button
          className="secondary"
          disabled={page <= 1}
          onClick={() => onChange(page - 1)}
        >
          Previous
        </button>
        <button
          className="secondary"
          disabled={page * 20 >= total}
          onClick={() => onChange(page + 1)}
        >
          Next
        </button>
      </div>
    </div>
  );
}
