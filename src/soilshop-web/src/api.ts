let token: string | null = null;
export class ApiError extends Error {
  constructor(message: string, public status: number, public quote?: import("./types").TradeQuote) {
    super(message);
  }
}
export function setToken(value: string | null) {
  token = value;
}
export async function api<T>(
  path: string,
  body?: unknown,
  method = "POST",
): Promise<T> {
  const response = await fetch("/api" + path, {
    method: body === undefined && method === "POST" ? "GET" : method,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
    },
    ...(body !== undefined ? { body: JSON.stringify(body) } : {}),
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    if (response.status === 401 && path !== "/auth/login")
      window.dispatchEvent(new Event("session-expired"));
    const errors = problem?.errors
      ? Object.values(problem.errors).flat().join(" ")
      : null;
    throw new ApiError(
      errors ||
        problem?.title ||
        (response.status === 429
          ? "Too many login attempts. Please wait a minute."
          : "Could not complete the request. Please retry."),
      response.status,
      problem?.quote,
    );
  }
  return response.status === 204
    ? (undefined as T)
    : (response.json() as Promise<T>);
}
