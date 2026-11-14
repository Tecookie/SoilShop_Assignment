const paths: Record<string, string> = {
  dashboard: "M3 3h7v7H3z M14 3h7v4h-7z M14 11h7v10h-7z M3 14h7v7H3z",
  leaf: "M20 4c-9-2-16 2-15 9 1 7 11 8 14 1 1-3 1-7 1-10Z M5 20 16 9 M10 15v-4 M10 15h5",
  inventory: "M3 7 12 3l9 4v10l-9 4-9-4Z M3 7l9 4 9-4 M12 11v10 M7 5l10 4",
  receipts: "M4 14v6h16v-6 M12 3v12 M7 10l5 5 5-5",
  sales: "M4 14v6h16v-6 M12 15V3 M7 8l5-5 5 5",
  movements: "M3 7h17 M16 3l4 4-4 4 M21 17H4 M8 13l-4 4 4 4",
  replenishment:
    "M20 7v5h-5 M4 17v-5h5 M6 6a8 8 0 0 1 14 6 M18 18a8 8 0 0 1-14-6",
  catalogue: "M4 4h6v6H4z M14 4h6v6h-6z M4 14h6v6H4z M14 14h6v6h-6z",
  counts: "M9 4h6v4H9z M9 6H5v15h14V6h-4 M8 13l2 2 5-5 M8 18h7",
  reports: "M5 3h10l4 4v14H5Z M14 3v5h5 M9 17v-3 M12 17v-6 M15 17v-4",
  backups:
    "M20 6c0 2-4 3-8 3S4 8 4 6s4-3 8-3 8 1 8 3Z M4 6v12c0 2 4 3 8 3s8-1 8-3V6 M4 12c0 2 4 3 8 3s8-1 8-3",
  suppliers:
    "M3 7h11v11H3z M14 11h4l3 4v3h-7 M6 18a2 2 0 1 0 4 0 M16 18a2 2 0 1 0 4 0",
  "sku-admin": "M4 7h16 M4 17h16 M8 4v6 M16 14v6 M4 4v16 M20 4v16",
  accounts:
    "M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z M17 3a4 4 0 0 1 0 8 M22 21v-2a4 4 0 0 0-3-4",
  menu: "M4 6h16 M4 12h16 M4 18h16",
  close: "m6 6 12 12 M18 6 6 18",
  logout: "M9 4H4v16h5 M9 12h12 M16 7l5 5-5 5",
  check: "m5 12 4 4L19 6",
  alert: "m12 3 10 18H2Z M12 9v4 M12 17h.01",
  search: "M17 10a7 7 0 1 1-14 0 7 7 0 0 1 14 0Z m-2 5 6 6",
  arrow: "M4 12h16 M14 6l6 6-6 6",
  lock: "M5 10h14v11H5z M8 10V6a4 4 0 0 1 8 0v4 M12 14v3",
  eye: "M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z",
};
export function Icon({
  name,
  className = "",
}: {
  name: string;
  className?: string;
}) {
  return (
    <svg
      className={`icon ${className}`}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d={paths[name] ?? paths.inventory} />
    </svg>
  );
}
