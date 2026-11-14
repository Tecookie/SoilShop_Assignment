import { useState } from "react";
import type { FormEvent } from "react";
import { api, setToken } from "../../shared/api/client";
import { Field, Notice } from "../../shared/components";
import type { Login, User } from "./types";
import { Icon } from "../../shared/components/Icon";
export function LoginPage({
  onLogin,
  message,
}: {
  onLogin: (user: User) => void;
  message?: string;
}) {
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError("");
    const form = new FormData(event.currentTarget);
    try {
      const result = await api<Login>("/auth/login", {
        username: form.get("username"),
        password: form.get("password"),
      });
      setToken(result.token);
      onLogin(result.user);
    } catch (error) {
      setError((error as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="login-layout">
      <section className="login-story">
        <div className="sidebar-brand">
          <span className="brand-symbol">
            <Icon name="leaf" />
          </span>
          <div className="wordmark">
            Soil<span>shop</span>
            <small>THE SHOP WORKSPACE</small>
          </div>
        </div>
        <p className="eyebrow">THE COUNTER WORKSPACE</p>
        <h1>
          Every bag.
          <br />
          Accounted for.
        </h1>
        <p>
          Receive pre-packed soil, record counter sales and keep a clear trail
          of stock movements.
        </p>
        <div className="login-capabilities">
          <div>
            <Icon name="inventory" />
            <span>
              <strong>A clearer view of stock</strong>
              <small>Know what is on hand, down to the bag.</small>
            </span>
          </div>
          <div>
            <Icon name="receipts" />
            <span>
              <strong>Every transaction, connected</strong>
              <small>From supplier receipts to counter sales.</small>
            </span>
          </div>
          <div>
            <Icon name="reports" />
            <span>
              <strong>Make the next move with confidence</strong>
              <small>Reviewed counts and practical shop reports.</small>
            </span>
          </div>
        </div>
        <div className="login-foot">INTERNAL OPERATIONS · ONE SHOP</div>
      </section>
      <section className="login-panel">
        <form onSubmit={submit}>
          <span className="login-lock">
            <Icon name="lock" />
          </span>
          <p className="eyebrow">STAFF ACCESS</p>
          <h2>Welcome back.</h2>
          <p className="muted">Sign in to your shop workspace.</p>
          <Notice error={error} text={message} />
          <Field label="Username">
            <input
              name="username"
              required
              maxLength={100}
              autoComplete="username"
              autoFocus
              placeholder="Enter your username"
              disabled={busy}
            />
          </Field>
          <div className="password-field">
            <Field label="Password">
              <input
                name="password"
                type={showPassword ? "text" : "password"}
                required
                maxLength={200}
                autoComplete="current-password"
                placeholder="Enter your password"
                disabled={busy}
              />
            </Field>
            <button
              className="password-toggle"
              type="button"
              aria-label={showPassword ? "Hide password" : "Show password"}
              aria-pressed={showPassword}
              onClick={() => setShowPassword((x) => !x)}
            >
              <Icon name="eye" />
            </button>
          </div>
          <button disabled={busy} type="submit">
            {busy ? (
              "Signing in…"
            ) : (
              <>
                Sign in <Icon name="arrow" />
              </>
            )}
          </button>
          <p className="login-note">
            Accounts are issued by the administrator. Your session ends when
            this page is reloaded.
          </p>
        </form>
      </section>
    </main>
  );
}
