import { useState } from "react";
import type { FormEvent } from "react";
import { api, setToken } from "./api";
import { Field, Notice } from "./components";
import type { Login, User } from "./types";
export function LoginPage({ onLogin }: { onLogin: (user: User) => void }) {
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
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
        <div className="wordmark">
          SOIL<span>SHOP</span>
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
        <div className="login-foot">INTERNAL OPERATIONS · ONE SHOP</div>
      </section>
      <section className="login-panel">
        <form onSubmit={submit}>
          <p className="eyebrow">STAFF ACCESS</p>
          <h2>Welcome to the stockroom</h2>
          <p className="muted">Sign in with your shop account.</p>
          <Notice error={error} />
          <Field label="Username">
            <input
              name="username"
              required
              maxLength={100}
              autoComplete="username"
              autoFocus
            />
          </Field>
          <Field label="Password">
            <input
              name="password"
              type="password"
              required
              maxLength={200}
              autoComplete="current-password"
            />
          </Field>
          <button disabled={busy} type="submit">
            {busy ? "Signing in…" : "Sign in →"}
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
