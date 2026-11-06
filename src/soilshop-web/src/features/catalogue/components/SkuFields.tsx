import { Field } from "../../../shared/components";
import type { Product, Supplier } from "../types";
export function SkuFields({
  products,
  suppliers,
}: {
  products: Product[];
  suppliers: Supplier[];
}) {
  return (
    <>
      <Field label="Product" hint="Products on this catalogue page.">
        <select name="productId" required defaultValue="">
          <option value="" disabled>
            Select product
          </option>
          {products
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
  );
}
