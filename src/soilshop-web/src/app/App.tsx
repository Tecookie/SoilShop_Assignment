import { AdvicePage } from "../features/advice/AdvicePage";
import { useEffect, useRef, useState } from "react";
import { Icon } from "../shared/components/Icon";
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
  admin?: boolean;
}[] = [
  { id: "dashboard", label: "Dashboard" },
  { id: "advice", label: "Tìm đất phù hợp" },
  { id: "inventory", label: "Inventory" },
  { id: "receipts", label: "Purchase receipts" },
  { id: "sales", label: "Sales" },
  { id: "movements", label: "Stock movements" },
  { id: "replenishment", label: "Replenishment" },
  { id: "catalogue", label: "Catalogue", admin: true },
  { id: "counts", label: "Stock counts" },
  { id: "reports", label: "Daily summaries" },
  { id: "backups", label: "Database backups", admin: true },
  { id: "suppliers", label: "Suppliers", admin: true },
  {
    id: "sku-admin",
    label: "SKU prices & stock rules",
    admin: true,
  },
  { id: "accounts", label: "Staff accounts", admin: true },
];
export function App() {
  const [navOpen, setNavOpen] = useState(false);
  const menuButton = useRef<HTMLButtonElement>(null);
  const sidebar = useRef<HTMLElement>(null);
  const [user, setUser] = useState<User | null>(null);
  const [view, setView] = useState<View | ExtraView>("inventory");
  const [sessionMessage, setSessionMessage] = useState("");
  function logout() {
    setNavOpen(false);
    setToken(null);
    setUser(null);
    setView("inventory");
  }
  useEffect(() => {
    if (!navOpen) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    sidebar.current?.querySelector<HTMLButtonElement>("button")?.focus();
    const close = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setNavOpen(false);
        menuButton.current?.focus();
      }
      if (e.key === "Tab") {
        const buttons =
          sidebar.current?.querySelectorAll<HTMLButtonElement>("button");
        if (!buttons?.length) return;
        const first = buttons[0],
          last = buttons[buttons.length - 1];
        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    };
    const resized = () => {
      if (window.innerWidth > 900) setNavOpen(false);
    };
    window.addEventListener("keydown", close);
    window.addEventListener("resize", resized);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", close);
      window.removeEventListener("resize", resized);
    };
  }, [navOpen]);
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
      {navOpen && (
        <button
          className="nav-backdrop"
          aria-label="Close navigation"
          tabIndex={-1}
          onClick={() => {
            setNavOpen(false);
            menuButton.current?.focus();
          }}
        />
      )}
      <aside
        id="app-navigation"
        ref={sidebar}
        className={`sidebar ${navOpen ? "is-open" : ""}`}
      >
        <div className="sidebar-brand">
          <span className="brand-symbol">
            <Icon name="leaf" />
          </span>
          <div className="wordmark">
            Soil<span>shop</span>
            <small>THE SHOP WORKSPACE</small>
          </div>
          <button
            className="mobile-menu sidebar-close"
            aria-label="Close menu"
            onClick={() => {
              setNavOpen(false);
              menuButton.current?.focus();
            }}
          >
            <Icon name="close" />
          </button>
        </div>
        <nav aria-label="Main navigation">
          {[
            { label: "WORKSPACE", ids: ["dashboard", "advice"] },
            {
              label: "STOCK & TRADING",
              ids: [
                "inventory",
                "receipts",
                "sales",
                "movements",
                "replenishment",
                "counts",
              ],
            },
            {
              label: "REPORTS & MANAGEMENT",
              ids: [
                "reports",
                "catalogue",
                "suppliers",
                "sku-admin",
                "accounts",
                "backups",
              ],
            },
          ].map((group) => (
            <div className="nav-group" key={group.label}>
              <p className="sidebar-label">{group.label}</p>
              {views
                .filter(
                  (v) =>
                    group.ids.includes(v.id) &&
                    (!v.admin || user.role === "Admin"),
                )
                .sort(
                  (a, b) => group.ids.indexOf(a.id) - group.ids.indexOf(b.id),
                )
                .map((v) => (
                  <button
                    key={v.id}
                    className={view === v.id ? "nav-item active" : "nav-item"}
                    aria-current={view === v.id ? "page" : undefined}
                    onClick={() => {
                      setView(v.id);
                      setNavOpen(false);
                    }}
                  >
                    <Icon name={v.id === "advice" ? "leaf" : v.id} />
                    {v.label}
                  </button>
                ))}
            </div>
          ))}
        </nav>
        <div className="sidebar-foot">
          <Icon name="lock" />
          <div>
            <strong>One shop. All together.</strong>
            <p>Stock, sales & everyday operations.</p>
          </div>
        </div>
      </aside>
      <div className="workspace-body">
        <header className="topbar">
          <div className="topbar-location">
            <button
              ref={menuButton}
              className="mobile-menu"
              aria-label="Open menu"
              aria-expanded={navOpen}
              aria-controls="app-navigation"
              onClick={() => setNavOpen(true)}
            >
              <Icon name="menu" />
            </button>
            <span className="breadcrumb">
              Workspace <span aria-hidden="true">/</span>{" "}
              <strong>{views.find((v) => v.id === view)?.label}</strong>
            </span>
          </div>
          <div className="topbar-account">
            <span className="user-avatar" aria-hidden="true">
              {user.displayName
                .trim()
                .split(/\s+/)
                .slice(-2)
                .map((x) => x[0])
                .join("")}
            </span>
            <span className="user-role">
              <strong>{user.displayName}</strong>
              <small>
                {user.role === "Admin" ? "Administrator" : "Shop employee"}
              </small>
            </span>
            <button className="secondary signout" onClick={logout}>
              <Icon name="logout" />
              <span>Sign out</span>
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
