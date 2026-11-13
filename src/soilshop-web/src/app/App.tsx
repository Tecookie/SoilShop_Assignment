import { AdvicePage } from "../features/advice/AdvicePage";
import { useEffect, useState } from "react";
import { setToken } from "../shared/api/client";
import { LoginPage } from "../features/authentication/LoginPage";
import { InventoryPage } from "../features/inventory/InventoryPage";
import { CataloguePage } from "../features/catalogue/CataloguePage";
import { TradingPage } from "../features/trading/TradingPage";
import { OperationsPage } from "../features/inventory/OperationsPage";
import type { User } from "../features/authentication/types";
import { StockCountsPage } from "../features/inventory/StockCountsPage";
import { ReportsPage } from "../features/operations/ReportsPage";
import { BackupsPage } from "../features/operations/BackupsPage";
import { AccountsPage } from "../features/administration/AccountsPage";
import { SuppliersPage } from "../features/administration/SuppliersPage";
import { SkuAdminPage } from "../features/administration/SkuAdminPage";
import { DashboardPage } from "../features/operations/DashboardPage";
type View =
  | "inventory"
  | "receipts"
  | "sales"
  | "movements"
  | "replenishment"
  | "catalogue"
  | "advice";
type ExtraView =
  | "dashboard"
  | "counts"
  | "reports"
  | "backups"
  | "accounts"
  | "suppliers"
  | "sku-admin";
const views: {
  id: View | ExtraView;
  label: string;
  icon: string;
  admin?: boolean;
}[] = [
  { id: "dashboard", label: "Dashboard", icon: "▤" },
  { id: "advice", label: "Tìm đất phù hợp", icon: "✧" },
  { id: "inventory", label: "Inventory", icon: "▦" },
  { id: "receipts", label: "Purchase receipts", icon: "↙" },
  { id: "sales", label: "Sales", icon: "↗" },
  { id: "movements", label: "Stock movements", icon: "≡" },
  { id: "replenishment", label: "Replenishment", icon: "↻" },
  { id: "catalogue", label: "Catalogue", icon: "◇", admin: true },
  { id: "counts", label: "Stock counts", icon: "☷" },
  { id: "reports", label: "Daily summaries", icon: "▤" },
  { id: "backups", label: "Database backups", icon: "▣", admin: true },
  { id: "suppliers", label: "Suppliers", icon: "⇄", admin: true },
  {
    id: "sku-admin",
    label: "SKU prices & stock rules",
    icon: "⊞",
    admin: true,
  },
  { id: "accounts", label: "Staff accounts", icon: "♙", admin: true },
];
export function App() {
  const [user, setUser] = useState<User | null>(null);
  const [view, setView] = useState<View | ExtraView>("inventory");
  const [sessionMessage, setSessionMessage] = useState("");
  function logout() {
    setToken(null);
    setUser(null);
    setView("inventory");
  }
  useEffect(() => {
    const expired = () => {
      logout();
      setSessionMessage(
        "Your session expired or your account changed. Please sign in again.",
      );
    };
    window.addEventListener("session-expired", expired);
    return () => window.removeEventListener("session-expired", expired);
  }, []);
  if (!user)
    return (
      <LoginPage
        message={sessionMessage}
        onLogin={(u) => {
          setUser(u);
          setSessionMessage("");
        }}
      />
    );
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
          {view === "advice" && <AdvicePage onSale={() => setView("sales")} />}
          {view === "inventory" && <InventoryPage />}
          {view === "dashboard" && <DashboardPage />}
          {view === "counts" && (
            <StockCountsPage isAdmin={user.role === "Admin"} />
          )}
          {view === "reports" && <ReportsPage />}
          {view === "backups" && user.role === "Admin" && <BackupsPage />}
          {view === "accounts" && user.role === "Admin" && <AccountsPage />}
          {view === "suppliers" && user.role === "Admin" && <SuppliersPage />}
          {view === "sku-admin" && user.role === "Admin" && <SkuAdminPage />}
          {view === "catalogue" && user.role === "Admin" && <CataloguePage />}
          {(view === "receipts" || view === "sales") && (
            <TradingPage
              key={view}
              receipt={view === "receipts"}
              isAdmin={user.role === "Admin"}
            />
          )}
          {(view === "movements" || view === "replenishment") && (
            <OperationsPage
              key={view}
              suggestions={view === "replenishment"}
              onReceipt={() => setView("receipts")}
            />
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
