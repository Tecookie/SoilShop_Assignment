import type { FormEvent } from "react";
import { Field } from "../../../shared/components";
import type { Product, Supplier } from "../types";
import { ProductFields } from "./ProductFields";
import { SkuFields } from "./SkuFields";
export function CatalogueForm({
  form,
  editing,
  products,
  suppliers,
  busy,
  submit,
  onClose,
}: {
  form: "product" | "sku" | "supplier";
  editing: Product | null;
  products: Product[];
  suppliers: Supplier[];
  busy: boolean;
  submit: (e: FormEvent<HTMLFormElement>) => void;
  onClose: () => void;
}) {
  return (
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
          {form === "product" && <ProductFields editing={editing} />}
          {form === "supplier" && (
            <Field label="Contact note (optional)">
              <input name="contactNote" maxLength={2000} />
            </Field>
          )}
          {form === "sku" && (
            <SkuFields products={products} suppliers={suppliers} />
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
              onClose();
            }}
          >
            Close form
          </button>
        </div>
      </form>
    </section>
  );
}
