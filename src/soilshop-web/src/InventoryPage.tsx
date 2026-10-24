import { useEffect, useState } from "react";
import { api } from "./api";
import { Empty, Notice, Pagination } from "./components";
import type { PageResult, Product, Sku } from "./types";
import { money, productTypes, productTypeName } from "./types";
export function InventoryPage() {
  const [search, setSearch] = useState("");
  const [lowOnly, setLowOnly] = useState(false);
  const [productType, setProductType] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PageResult<Sku>>();
  const [products, setProducts] = useState<Product[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [refresh, setRefresh] = useState(0);
  useEffect(() => {
    const controller = { cancelled: false };
    setLoading(true);
    setError("");
    const timeout = setTimeout(() => {
      Promise.all([
        api<PageResult<Sku>>(
          `/skus?search=${encodeURIComponent(search)}&lowOnly=${lowOnly}&page=${page}${productType ? `&productType=${productType}` : ""}`,
        ),
        api<PageResult<Product>>("/products?pageSize=100"),
      ])
        .then(([result, catalogue]) => {
          if (!controller.cancelled) {
            setData(result);
            setProducts(catalogue.items);
          }
        })
        .catch((error) => {
          if (!controller.cancelled) setError(error.message);
        })
        .finally(() => {
          if (!controller.cancelled) setLoading(false);
        });
    }, 200);
    return () => {
      controller.cancelled = true;
      clearTimeout(timeout);
    };
  }, [search, lowOnly, page, refresh, productType]);
  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">LIVE STOCK</p>
          <h1>Inventory</h1>
          <p className="muted">
            Sealed bags, counted by package. Draft documents do not reserve
            stock.
          </p>
        </div>
        <button className="secondary" onClick={() => setRefresh((x) => x + 1)}>
          Refresh stock
        </button>
      </div>
      <div className="panel">
        <div className="toolbar">
          <input
            aria-label="Search inventory"
            type="search"
            placeholder="Search product or SKU code…"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <select
            className="type-filter"
            aria-label="Filter inventory by type"
            value={productType}
            onChange={(event) => {
              setProductType(event.target.value);
              setPage(1);
            }}
          >
            <option value="">All product types</option>
            {productTypes.map((type) => (
              <option key={type.code} value={type.code}>
                {type.name}
              </option>
            ))}
          </select>
          <label className="check">
            <input
              type="checkbox"
              checked={lowOnly}
              onChange={(e) => {
                setLowOnly(e.target.checked);
                setPage(1);
              }}
            />
            Low stock only
          </label>
          <span className="muted">Unit: bags</span>
        </div>
        <Notice error={error} />
        <div className="table-wrap" aria-busy={loading}>
          <table>
            <thead>
              <tr>
                <th>Product / SKU</th>
                <th>Package</th>
                <th className="number">Price</th>
                <th className="number">On hand</th>
                <th>Stock status</th>
                <th className="number">Target</th>
              </tr>
            </thead>
            <tbody>
              {!loading &&
                data?.items.map((sku) => (
                  <tr key={sku.id}>
                    <td>
                      <strong>
                        {products.find((p) => p.id === sku.productId)?.name ??
                          `Product #${sku.productId}`}
                      </strong>
                      <small>{sku.code}</small>
                      <small>
                        {productTypeName(
                          products.find((p) => p.id === sku.productId)
                            ?.productType,
                        )}
                      </small>
                    </td>
                    <td>
                      {sku.packageAmount} {sku.packageUnit}
                    </td>
                    <td className="number">{money(sku.salePriceVnd)}</td>
                    <td className="number stock">{sku.stockQty}</td>
                    <td>
                      <span
                        className={`badge ${!sku.isActive ? "neutral" : sku.stockQty <= sku.lowStockThreshold ? "warning" : "good"}`}
                      >
                        {!sku.isActive
                          ? "Inactive"
                          : sku.stockQty === 0
                            ? "Out of stock"
                            : sku.stockQty <= sku.lowStockThreshold
                              ? "Low stock"
                              : "Available"}
                      </span>
                    </td>
                    <td className="number">{sku.targetStock}</td>
                  </tr>
                ))}
            </tbody>
          </table>
          {loading ? (
            <Empty>Loading inventory…</Empty>
          ) : (
            !data?.items.length && (
              <Empty>
                No matching SKUs. An Admin can add products and packaging in
                Catalogue.
              </Empty>
            )
          )}
        </div>
        {data && (
          <Pagination page={page} total={data.total} onChange={setPage} />
        )}
      </div>
    </>
  );
}
