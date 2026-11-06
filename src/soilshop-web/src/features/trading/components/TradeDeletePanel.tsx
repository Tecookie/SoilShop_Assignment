import type { Trade } from "../types";
export function TradeDeletePanel({
  deleting,
  busy,
  removeDraft,
  onClose,
}: {
  deleting: Trade;
  busy: boolean;
  removeDraft: () => void;
  onClose: () => void;
}) {
  return (
    <section className="panel form-panel" aria-label="Delete draft">
      <h2>Delete draft {deleting.code.slice(0, 12).toUpperCase()}?</h2>
      <p>Only this draft will be removed. It has not changed stock.</p>
      <div className="form-actions">
        <button disabled={busy} onClick={removeDraft}>
          Delete draft
        </button>
        <button className="secondary" disabled={busy} onClick={() => onClose()}>
          Keep draft
        </button>
      </div>
    </section>
  );
}
