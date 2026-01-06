import type { AxiosError } from "axios";
import api from "../services/api";

type IdentityError = { code?: string; description?: string };

function errorMessage(err: unknown, fallback: string) {
  const e = err as AxiosError<any>;
  const data = e?.response?.data;

  // Back: string
  if (typeof data === "string") return data;

  // Back: { message: "..." }
  if (data?.message) return String(data.message);

  // Back: Identity errors [{ code, description }, ...]
  if (Array.isArray(data)) {
    const msgs = (data as IdentityError[])
      .map((x) => x.description)
      .filter(Boolean);
    if (msgs.length) return msgs.join(" ");
  }

  // Axios / réseau
  if (e?.message) return e.message;

  return fallback;
}

export function setJwt(token: string) {
  localStorage.setItem("jwt", token);
}

export function getJwt(): string | null {
  return localStorage.getItem("jwt");
}

export async function register(email: string, password: string) {
  try {
    const res = await api.post("/auth/register", { email, password });
    return res.data;
  } catch (err) {
    throw new Error(errorMessage(err, "Inscription impossible."));
  }
}

export async function login(email: string, password: string) {
  try {
    const res = await api.post("/auth/login", { email, password });

    const data = res.data ?? {};
    const token = data.token ?? data.accessToken ?? data.jwt;

    if (!token) throw new Error("Réponse login: token introuvable.");

    setJwt(token);
    return data;
  } catch (err) {
    throw new Error(errorMessage(err, "Identifiants invalides."));
  }
}
