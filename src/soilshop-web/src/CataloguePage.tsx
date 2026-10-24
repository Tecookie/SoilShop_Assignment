import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { api } from "./api";
import { Empty, Field, Notice, Pagination } from "./components";
import type { PageResult, Product, Sku, Supplier } from "./types";
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
        <section className="panel form-panel">
          <h2>
            {editing
              ? "Edit product"
              : `New ${form === "sku" ? "packaging variant" : form}`}
          </h2>
          <form key={`${form}-${editing?.id ?? "new"}`} onSubmit={submit}>
            <div className="form-grid">
              <Field label="Code">
                <input
                  name="code"
                  maxLength={form === "sku" ? 50 : 40}
                  defaultValue={editing?.code}
                  required
                />
              </Field>
              {form !== "sku" && (
                <Field label="Name">
                  <input
                    name="name"
                    maxLength={200}
                    defaultValue={editing?.name}
                    required
                  />
                </Field>
              )}
              {form === "product" && (
                <>
                  <Field label="Product type">
                    <select
                      name="productType"
                      defaultValue={editing?.productType ?? "Unclassified"}
                    >
                      {productTypes.map((type) => (
                        <option key={type.code} value={type.code}>
                          {type.name}
                        </option>
                      ))}
                    </select>
                  </Field>
                  <Field label="Brand (optional)">
                    <input
                      name="brand"
                      maxLength={150}
                      defaultValue={editing?.brand ?? ""}
                    />
                  </Field>
                  <Field label="Ingredients (optional)">
                    <input
                      name="ingredients"
                      maxLength={5000}
                      defaultValue={editing?.ingredients ?? ""}
                    />
                  </Field>
                  <Field
                    label="Source reference (optional)"
                    hint="Record label or supplier information. Leave unknown data blank."
                  >
                    <input
                      name="sourceReference"
                      maxLength={2000}
                      defaultValue={editing?.sourceReference ?? ""}
                    />
                  </Field>
                  {editing && (
                    <label className="check">
                      <input
                        name="isActive"
                        type="checkbox"
                        defaultChecked={editing.isActive}
                      />
                      Active product
                    </label>
                  )}
                </>
              )}
              {form === "supplier" && (
                <Field label="Contact note (optional)">
                  <input name="contactNote" maxLength={2000} />
                </Field>
              )}
              {form === "sku" && (
                <>
                  <Field
                    label="Product"
                    hint="Products on this catalogue page."
                  >
                    <select name="productId" required defaultValue="">
                      <option value="" disabled>
                        Select product
                      </option>
                      {products?.items
                        .filter((p) => p.isActive)
                        .map((p) => (
                          <option key={p.id} value={p.id}>
                            {p.code} · {p.name}
                          </option>
                        ))}
                    </select>
                  </Field>
                  <Field label="Package amount">
                    <input
                      name="packageAmount"
                      type="number"
                      min="0.01"
                      max="1000000"
                      step="0.01"
                      required
                    />
                  </Field>
                  <Field label="Package unit">
                    <select name="packageUnit">
                      <option value="kg">kg</option>
                      <option value="L">L</option>
                    </select>
                  </Field>
                  <Field label="Sale price (VND)">
                    <input
                      name="salePriceVnd"
                      type="number"
                      min="1"
                      max="1000000000000"
                      step="1"
                      required
                    />
                  </Field>
                  <Field label="Low-stock threshold (bags)">
                    <input
                      name="threshold"
                      type="number"
                      min="0"
                      max="1000000"
                      step="1"
                      defaultValue="5"
                      required
                    />
                  </Field>
                  <Field label="Target stock (bags)">
                    <input
                      name="target"
                      type="number"
                      min="1"
                      max="1000000"
                      step="1"
                      defaultValue="30"
                      required
                    />
                  </Field>
                  <Field label="Preferred supplier (optional)">
                    <select name="supplierId" defaultValue="">
                      <option value="">Not assigned</option>
                      {suppliers
                        .filter((s) => s.isActive)
                        .map((s) => (
                          <option key={s.id} value={s.id}>
                            {s.name}
                          </option>
                        ))}
                    </select>
                  </Field>
                </>
              )}
            </div>
            <div className="form-actions">
              <button type="submit" disabled={busy}>
                {busy ? "Saving…" : "Save"}
              </button>
              <button
                type="button"
                className="secondary"
                disabled={busy}
                onClick={() => {
                  setForm(null);
                  setEditing(null);
                }}
              >
                Close form
              </button>
            </div>
          </form>
        </section>
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
