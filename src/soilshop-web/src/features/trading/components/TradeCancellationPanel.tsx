import type { FormEvent } from "react";
import type { Trade } from "../types";
import { Field } from "../../../shared/components";
export function TradeCancellationPanel({
  receipt,
  cancelling,
  reason,
  busy,
  onReasonChange,
  cancel,
  onClose,
}: {
  receipt: boolean;
  cancelling: Trade;
  reason: string;
  busy: boolean;
  onReasonChange: (v: string) => void;
  cancel: (e: FormEvent) => void;
  onClose: () => void;
}) {
  return (
    <section className="panel form-panel" aria-label="Cancel document">
      <h2>Cancel {cancelling.code.slice(0, 12).toUpperCase()} in full</h2>
      <p>
        {receipt
          ? "The original bag quantities will be deducted. All lines require sufficient current stock."
          : "The original bag quantities will be restored."}{" "}
        History and frozen prices remain.
      </p>
      <form onSubmit={cancel}>
        <Field label="Cancellation reason">
          <textarea
            required
            maxLength={500}
            value={reason}
            onChange={(e) => {
              onReasonChange(e.target.value);
            }}
          />
        </Field>
        <div className="form-actions">
          <button className="danger" disabled={busy || !reason.trim()}>
            Confirm cancellation
          </button>
          <button
            type="button"
            className="secondary"
            disabled={busy}
            onClick={() => onClose()}
          >
            Keep document
          </button>
        </div>
      </form>
    </section>
  );
}
