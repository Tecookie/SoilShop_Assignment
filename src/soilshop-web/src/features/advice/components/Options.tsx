export function Options({ values }: { values: Record<string, string> }) {
  return (
    <>
      <option value="">Chưa xác định / không chọn</option>
      {Object.entries(values).map(([v, label]) => (
        <option key={v} value={v}>
          {label}
        </option>
      ))}
    </>
  );
}
