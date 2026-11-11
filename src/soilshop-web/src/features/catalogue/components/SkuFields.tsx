import { Field } from "../../../shared/components";
import type { Product, Supplier } from "../types";
import { useState } from "react";
import { SearchSelect } from "../../../shared/components/SearchSelect";
export function SkuFields(_props: {
  products: Product[];
  suppliers: Supplier[];
}) {
  const [product, setProduct] = useState("");
  const [supplier, setSupplier] = useState("");
  return (
    <>
      <input type="hidden" name="productId" value={product} />
      <SearchSelect
        label="Product"
        resource="/products"
        value={product}
        onChange={setProduct}
      />
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
      <input type="hidden" name="supplierId" value={supplier} />
      <SearchSelect
        label="Preferred supplier (optional)"
        resource="/supplier-directory"
        value={supplier}
        onChange={setSupplier}
        required={false}
      />
    </>
  );
}
