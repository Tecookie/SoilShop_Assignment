import { Field } from "../../../shared/components";
import type { Product } from "../types";
import { productTypes } from "../types";
export function ProductFields({ editing }: { editing: Product | null }) {
  return (
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
  );
}
