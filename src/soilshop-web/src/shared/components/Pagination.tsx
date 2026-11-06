export function Pagination({
  page,
  total,
  onChange,
}: {
  page: number;
  total: number;
  onChange: (page: number) => void;
}) {
  return (
    <div className="pagination">
      <span>
        {total} records · page {page} of {Math.max(1, Math.ceil(total / 20))}
      </span>
      <div>
        <button
          className="secondary"
          disabled={page <= 1}
          onClick={() => onChange(page - 1)}
        >
          Previous
        </button>
        <button
          className="secondary"
          disabled={page * 20 >= total}
          onClick={() => onChange(page + 1)}
        >
          Next
        </button>
      </div>
    </div>
  );
}
