import { useEffect, useState } from "react";
import { setToken } from "./api";
import { LoginPage } from "./LoginPage";
import { InventoryPage } from "./InventoryPage";
import { CataloguePage } from "./CataloguePage";
import { TradingPage } from "./TradingPage";
import { OperationsPage } from "./OperationsPage";
import type { User } from "./types";
type View =
  | "inventory"
  | "receipts"
  | "sales"
  | "movements"
  | "replenishment"
  | "catalogue";
const views: { id: View; label: string; icon: string; admin?: boolean }[] = [
  { id: "inventory", label: "Inventory", icon: "▦" },
  { id: "receipts", label: "Purchase receipts", icon: "↙" },
  { id: "sales", label: "Sales", icon: "↗" },
  { id: "movements", label: "Stock movements", icon: "≡" },
  { id: "replenishment", label: "Replenishment", icon: "↻" },
  { id: "catalogue", label: "Catalogue", icon: "◇", admin: true },
];
export function App() {
  const [user, setUser] = useState<User | null>(null);
  const [view, setView] = useState<View>("inventory");
  function logout() {
    setToken(null);
    setUser(null);
    setView("inventory");
  }
  useEffect(() => {
    window.addEventListener("session-expired", logout);
    return () => window.removeEventListener("session-expired", logout);
  }, []);
  if (!user) return <LoginPage onLogin={setUser} />;
  return (
    <div className="workspace">
      <a href="#main" className="skip-link">
        Skip to content
      </a>
      <aside className="sidebar">
        <div className="wordmark">
          SOIL<span>SHOP</span>
        </div>
        <p className="sidebar-label">STOCKROOM</p>
        <nav aria-label="Main navigation">
          {views
            .filter((v) => !v.admin || user.role === "Admin")
            .map((v) => (
              <button
                key={v.id}
                className={view === v.id ? "nav-item active" : "nav-item"}
                aria-current={view === v.id ? "page" : undefined}
                onClick={() => setView(v.id)}
              >
                <span aria-hidden="true">{v.icon}</span>
                {v.label}
              </button>
            ))}
        </nav>
        <div className="sidebar-foot">
          <span className="badge outline">INTERNAL WORKSPACE</span>
          <p>
            Pre-packed bags.
            <br />
            One shop. One stock location.
          </p>
        </div>
      </aside>
      <div className="workspace-body">
        <header className="topbar">
          <span>
            Operations <span aria-hidden="true">/</span>{" "}
            {views.find((v) => v.id === view)?.label}
          </span>
          <div>
            <span className="user-role">
              {user.displayName} · {user.role}
            </span>
            <button className="secondary" onClick={logout}>
              Sign out
            </button>
          </div>
        </header>
        <main id="main" tabIndex={-1}>
          {view === "inventory" && <InventoryPage />}
          {view === "catalogue" && user.role === "Admin" && <CataloguePage />}
          {(view === "receipts" || view === "sales") && (
            <TradingPage key={view} receipt={view === "receipts"} />
          )}
          {(view === "movements" || view === "replenishment") && (
            <OperationsPage key={view} suggestions={view === "replenishment"} />
          )}
        </main>
        <footer className="app-footer">
          <span>SOIL SHOP · PRN232</span>
          <span>Stock is authoritative at confirmation · Times in Vietnam</span>
        </footer>
      </div>
    </div>
  );
}
