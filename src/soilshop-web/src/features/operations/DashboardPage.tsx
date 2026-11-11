import { useEffect, useState } from "react";
import { api } from "../../shared/api/client";
import { Field, Notice } from "../../shared/components";
import { money, time } from "../../shared/lib/format";

interface Bucket {
  from: string;
  to: string;
  saleCount: number;
  soldBags: number;
  saleVnd: number;
  receiptCount: number;
  receivedBags: number;
  receiptVnd: number;
}
interface Dashboard {
  from: string;
  to: string;
  groupBy: string;
  generatedAt: string;
  lowStockSkus: number;
  totals: Bucket;
  periods: Bucket[];
}
const vietnamToday = () =>
  new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Ho_Chi_Minh",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date());
function offsetDate(value: string, days: number) {
  const d = new Date(`${value}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}
export function DashboardPage() {
  const today = vietnamToday();
  const [from, setFrom] = useState(() => offsetDate(today, -6));
  const [to, setTo] = useState(today);
  const [groupBy, setGroupBy] = useState("day");
  const [data, setData] = useState<Dashboard>();
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  async function load(start = from, end = to, group = groupBy) {
    setLoading(true);
    setError("");
    try {
      setData(
        await api<Dashboard>(
          `/dashboard?from=${start}&to=${end}&groupBy=${group}`,
        ),
      );
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => {
    void load(offsetDate(vietnamToday(), -6), vietnamToday(), "day");
  }, []);
  const maximum = Math.max(
    1,
    ...(data?.periods.flatMap((x) => [x.saleVnd, x.receiptVnd]) ?? []),
  );
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">SHOP OVERVIEW</p>
          <h1>Dashboard</h1>
          <p className="muted">
            Confirmed sales and receipts by Vietnam business date. Cancelled
            documents are excluded.
          </p>
        </div>
      </div>
      <Notice error={error} />
      <section className="panel form-panel">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            void load();
          }}
        >
          <div className="form-grid">
            <Field label="From date">
              <input
                type="date"
                value={from}
                min="2000-01-01"
                max={today}
                onChange={(e) => setFrom(e.target.value)}
                required
              />
            </Field>
            <Field label="Through date">
              <input
                type="date"
                value={to}
                min={from}
                max={today}
                onChange={(e) => setTo(e.target.value)}
                required
              />
            </Field>
            <Field label="Group by">
              <select
                value={groupBy}
                onChange={(e) => setGroupBy(e.target.value)}
              >
                <option value="day">Day</option>
                <option value="week">Week · Monday to Sunday</option>
              </select>
            </Field>
          </div>
          <button disabled={loading}>
            {loading ? "Loading…" : "View statistics"}
          </button>{" "}
          <span className="muted">Maximum range: 366 days.</span>
        </form>
      </section>
      {loading && !data && <p role="status">Loading statistics…</p>}
      {data && (
        <>
          <p className="muted">
            {data.from} → {data.to} · Generated {time(data.generatedAt)} ·
            Low-stock count is current.
          </p>
          <div className="dashboard-metrics" aria-label="Overview metrics">
            <section>
              <span>Sales total</span>
              <strong>{money(data.totals.saleVnd)}</strong>
              <small>
                {data.totals.saleCount} sales · {data.totals.soldBags} bags
              </small>
            </section>
            <section>
              <span>Purchase total</span>
              <strong>{money(data.totals.receiptVnd)}</strong>
              <small>
                {data.totals.receiptCount} receipts · {data.totals.receivedBags}{" "}
                bags
              </small>
            </section>
            <section>
              <span>Low-stock SKUs now</span>
              <strong>{data.lowStockSkus}</strong>
              <small>Active products and variants</small>
            </section>
          </div>
          <section className="panel dashboard-chart">
            <h2>Sales and purchases by {data.groupBy}</h2>
            <p className="muted">
              These totals are based on current document status. Purchase total
              is not cost of goods sold; this is not a profit report.
            </p>
            <div
              className="dashboard-bars"
              aria-label="Sales and purchase comparison"
            >
              {data.periods.map((x) => (
                <div className="dashboard-bar-row" key={x.from}>
                  <span>
                    {x.from}
                    {x.from !== x.to ? ` – ${x.to}` : ""}
                  </span>
                  <div>
                    <div
                      className="dashboard-bar sale"
                      style={{ width: `${(100 * x.saleVnd) / maximum}%` }}
                      title={`Sales ${money(x.saleVnd)}`}
                    />
                    <div
                      className="dashboard-bar purchase"
                      style={{ width: `${(100 * x.receiptVnd) / maximum}%` }}
                      title={`Purchases ${money(x.receiptVnd)}`}
                    />
                  </div>
                </div>
              ))}
            </div>
            <p className="muted">Blue = sales · Teal = purchase receipts</p>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Period</th>
                    <th>Sales</th>
                    <th>Sold bags</th>
                    <th>Sales total</th>
                    <th>Receipts</th>
                    <th>Received bags</th>
                    <th>Purchase total</th>
                  </tr>
                </thead>
                <tbody>
                  {data.periods.map((x) => (
                    <tr key={x.from}>
                      <td>
                        {x.from}
                        {x.from !== x.to ? ` – ${x.to}` : ""}
                      </td>
                      <td>{x.saleCount}</td>
                      <td>{x.soldBags}</td>
                      <td>{money(x.saleVnd)}</td>
                      <td>{x.receiptCount}</td>
                      <td>{x.receivedBags}</td>
                      <td>{money(x.receiptVnd)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </>
  );
}
