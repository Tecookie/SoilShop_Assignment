import type { Trade, TradeQuote } from "../types";
import type { Sku } from "../../catalogue/types";
import { money } from "../../../shared/lib/format";
export function TradeReviewPanel({
  receipt,
  review,
  skus,
  busy,
  confirm,
  onClose,
}: {
  receipt: boolean;
  review: { trade: Trade; quote: TradeQuote };
  skus: Sku[];
  busy: boolean;
  confirm: (trade: Trade) => void;
  onClose: () => void;
}) {
  return (
    <section className="panel form-panel" aria-label="Review quote">
      <h2>Review {review.trade.code.slice(0, 12).toUpperCase()}</h2>
      <ul className="document-lines">
        {review.quote.lines.map((line) => (
          <li key={line.skuId}>
            {review.trade.lines.find((s) => s.skuId === line.skuId)?.skuCode ??
              skus.find((s) => s.id === line.skuId)?.code ??
              `SKU #${line.skuId}`}{" "}
            · {line.quantity} bags × {money(line.unitPriceVnd)}
          </li>
        ))}
      </ul>
      <p>
        <strong>Total: {money(review.quote.totalVnd)}</strong>
      </p>
      <p className="muted">
        Review before confirming. A draft does not reserve stock. Changed prices
        require another review.
      </p>
      <div className="form-actions">
        <button disabled={busy} onClick={() => confirm(review.trade)}>
          Confirm {receipt ? "receipt" : "sale"}
        </button>
        <button disabled={busy} className="secondary" onClick={() => onClose()}>
          Close quote
        </button>
      </div>
    </section>
  );
}
