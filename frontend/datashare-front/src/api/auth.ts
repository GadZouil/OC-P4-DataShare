const API_URL = import.meta.env.VITE_API_URL;

type Json = Record<string, any>;

async function readJson(res: Response): Promise<Json> {
  const ct = res.headers.get("content-type") ?? "";
  if (!ct.includes("application/json")) return {};
  return await res.json();
}

export function setJwt(token: string) {
  localStorage.setItem("jwt", token);
}

export function getJwt(): string | null {
  return localStorage.getItem("jwt");
}

export async function register(email: string, password: string) {
  const res = await fetch(`${API_URL}/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  const data = await readJson(res);
  if (!res.ok) throw new Error(data.message ?? "Inscription impossible.");
  return data;
}

export async function login(email: string, password: string) {
  const res = await fetch(`${API_URL}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  const data = await readJson(res);
  if (!res.ok) throw new Error(data.message ?? "Identifiants invalides.");

  const token = data.token ?? data.accessToken ?? data.jwt;
  if (!token) throw new Error("Réponse login: token introuvable.");
  setJwt(token);

  return data;
}
