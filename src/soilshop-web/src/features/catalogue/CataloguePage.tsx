import { CatalogueForm } from "./components/CatalogueForm";
import { SoilMetadataPanel } from "../advice/components/SoilMetadataPanel";
import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { api } from "../../shared/api/client";
import { Empty, Field, Notice, Pagination } from "../../shared/components";
import type { PageResult } from "../../shared/types/paging";
import type { Product, Sku, Supplier } from "./types";
import { productTypes, productTypeName } from "./types";

export function CataloguePage() {
  const [products, setProducts] = useState<PageResult<Product>>();
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [page, setPage] = useState(1);
  const [productType, setProductType] = useState("");
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [form, setForm] = useState<"product" | "sku" | "supplier" | null>(null);
  const [metadataId, setMetadataId] = useState<number | null>(null);
  const [editing, setEditing] = useState<Product | null>(null);
  const load = useCallback(async () => {
    const [p, s] = await Promise.all([
      api<PageResult<Product>>(
        `/products?page=${page}${productType ? `&productType=${productType}` : ""}`,
      ),
      api<Supplier[]>("/suppliers"),
    ]);
    setProducts(p);
    setSuppliers(s);
  }, [page, productType]);
  useEffect(() => {
    load().catch((e) => setError(e.message));
  }, [load]);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const element = event.currentTarget;
    const data = new FormData(element);
    setBusy(true);
    setError("");
    setNotice("");
    const value = (name: string) => String(data.get(name) ?? "");
    try {
      if (form === "product")
        await api<Product>(
          editing ? `/products/${editing.id}` : "/products",
          {
            code: value("code"),
            name: value("name"),
            productType: value("productType"),
            brand: value("brand") || null,
            ingredients: value("ingredients") || null,
            sourceReference: value("sourceReference") || null,
            expectedVersion: editing?.version ?? null,
            isActive: editing ? data.get("isActive") === "on" : true,
          },
          editing ? "PUT" : "POST",
        );
      if (form === "supplier")
        await api<Supplier>("/suppliers", {
          code: value("code"),
          name: value("name"),
          contactNote: value("contactNote") || null,
        });
      if (form === "sku")
        await api<Sku>("/skus", {
          productId: Number(value("productId")),
          code: value("code"),
          packageAmount: Number(value("packageAmount")),
          packageUnit: value("packageUnit"),
          salePriceVnd: Number(value("salePriceVnd")),
          lowStockThreshold: Number(value("threshold")),
          targetStock: Number(value("target")),
          preferredSupplierId: value("supplierId")
            ? Number(value("supplierId"))
            : null,
        });
      setNotice(
        "Saved. New SKUs start at zero stock; confirm a receipt to receive bags.",
      );
      setForm(null);
      setEditing(null);
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
          <p className="eyebrow">MASTER DATA · ADMIN</p>
          <h1>Catalogue</h1>
          <p className="muted">
            Products describe the soil. SKUs define the sealed package and its
            price.
          </p>
        </div>
      </div>
      <Notice error={error} text={notice} />
      {metadataId !== null && (
        <SoilMetadataPanel
          key={metadataId}
          productId={metadataId}
          onChanged={load}
          onClose={() => setMetadataId(null)}
        />
      )}
      <div className="action-bar">
        <Field label="Filter catalogue by type">
          <select
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
        </Field>
      </div>
      <div className="action-bar">
        {(["product", "sku", "supplier"] as const).map((kind) => (
          <button
            key={kind}
            className={form === kind ? "" : "secondary"}
            onClick={() => {
              setForm(kind);
              setEditing(null);
              setError("");
            }}
          >
            +{" "}
            {kind === "sku"
              ? "Packaging / SKU"
              : kind[0].toUpperCase() + kind.slice(1)}
          </button>
        ))}
      </div>
      {form && (
        <CatalogueForm
          form={form}
          editing={editing}
          products={products?.items ?? []}
          suppliers={suppliers}
          busy={busy}
          submit={submit}
          onClose={() => {
            setForm(null);
            setEditing(null);
          }}
        />
      )}
      <section className="panel">
        <div className="section-heading">
          <h2>Products</h2>
          <span className="muted">
            Optional metadata stays unknown until supplied
          </span>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Product</th>
                <th>Brand</th>
                <th>Product type</th>
                <th>Source reference</th>
                <th>Status</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {products?.items.map((p) => (
                <tr key={p.id}>
                  <td>
                    <strong>{p.name}</strong>
                    <small>{p.code}</small>
                  </td>
                  <td>{p.brand ?? "Unknown"}</td>
                  <td>{productTypeName(p.productType)}</td>
                  <td>{p.sourceReference ?? "Not supplied"}</td>
                  <td>
                    <span
                      className={`badge ${p.isActive ? "good" : "neutral"}`}
                    >
                      {p.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td>
                    <button
                      className="secondary"
                      onClick={() => {
                        setEditing(p);
                        setForm("product");
                        setNotice("");
                      }}
                    >
                      Edit
                    </button>
                    <button
                      className="secondary"
                      onClick={() => {
                        setForm(null);
                        setMetadataId(p.id);
                        window.scrollTo({ top: 0, behavior: "smooth" });
                      }}
                    >
                      Thông số đất
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!products?.items.length && (
            <Empty>
              No products yet. Add a product, then a packaging variant.
            </Empty>
          )}
        </div>
        {products && (
          <Pagination page={page} total={products.total} onChange={setPage} />
        )}
      </section>
      <section className="panel">
        <div className="section-heading">
          <h2>Suppliers</h2>
          <span className="muted">Offline supply partners · first 100</span>
        </div>
        {suppliers.length ? (
          <ul className="supplier-list">
            {suppliers.map((s) => (
              <li key={s.id}>
                <strong>{s.name}</strong>
                <span>{s.code}</span>
                <span className="muted">{s.contactNote}</span>
              </li>
            ))}
          </ul>
        ) : (
          <Empty>No suppliers yet. Add one before receiving goods.</Empty>
        )}
      </section>
    </>
  );
}
