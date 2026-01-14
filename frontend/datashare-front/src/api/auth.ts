import api from "../services/api";
import type { AxiosError } from "axios";
import { ref } from "vue";

type IdentityError = { code: string; description?: string };

const JWT_STORAGE_KEY = "jwt";
const jwtRef = ref<string | null>(localStorage.getItem(JWT_STORAGE_KEY));
const USERNAME_KEY = "user_name";

window.addEventListener("storage", (e) => {
  if (e.key === JWT_STORAGE_KEY) jwtRef.value = e.newValue;
});

function mapIdentityCode(code: string): string | null {
  switch (code) {
    case "DuplicateUserName":
      return "Cette adresse email est déjà utilisée.";
    case "InvalidEmail":
      return "Adresse email invalide.";
    case "PasswordTooShort":
      return "Mot de passe : minimum 8 caractères.";
    case "PasswordRequiresNonAlphanumeric":
      return "Mot de passe : ajoute au moins un caractère spécial (ex: ! @ # ?).";
    case "PasswordRequiresDigit":
      return "Mot de passe : ajoute au moins un chiffre.";
    case "PasswordRequiresUpper":
      return "Mot de passe : ajoute au moins une majuscule.";
    case "PasswordRequiresLower":
      return "Mot de passe : ajoute au moins une minuscule.";
    default:
      return null;
  }
}

function identityErrorsToMessage(errors: IdentityError[]): string {
  const msgs = errors
    .map((e) => mapIdentityCode(e.code) ?? e.description)
    .filter((m): m is string => typeof m === "string" && m.trim().length > 0);

  return msgs.join("\n");
}

function getApiErrorMessage(err: unknown, fallback: string): string {
  const e = err as AxiosError<any>;
  const data = e?.response?.data;

  if (Array.isArray(data)) {
    const msg = identityErrorsToMessage(data as IdentityError[]);
    return msg || fallback;
  }

  const msg = data?.message ?? data?.error ?? e?.message;
  return typeof msg === "string" && msg.trim().length > 0 ? msg : fallback;
}

export function setJwt(token: string) {
  localStorage.setItem(JWT_STORAGE_KEY, token);
  jwtRef.value = token;
}

export function getJwt(): string | null {
  return jwtRef.value;
}

export function clearJwt() {
  localStorage.removeItem(JWT_STORAGE_KEY);
  jwtRef.value = null;
}

export function logout() {
  clearJwt();
}

export function isAuthenticated(): boolean {
  return !!jwtRef.value;
}

export async function register(email: string, password: string) {
  try {
    const res = await api.post("/auth/register", { email, password });
    return res.data;
  } catch (err) {
    throw new Error(getApiErrorMessage(err, "Inscription impossible."));
  }
}

export async function login(email: string, password: string) {
  try {
    const res = await api.post("/auth/login", { email, password });
    const data = res.data ?? {};
    const token = data.token ?? data.accessToken ?? data.jwt;

    if (typeof token !== "string" || token.length === 0) {
      throw new Error("Réponse login: token introuvable.");
    }

    setJwt(token);
    return data;
  } catch (err) {
    throw new Error(getApiErrorMessage(err, "Identifiants invalides."));
  }
}

export function setUsername(username: string) {
  const v = username?.trim();
  if (v) localStorage.setItem(USERNAME_KEY, v);
}

export function getUsername(): string | null {
  return localStorage.getItem(USERNAME_KEY);
}

export function clearUsername() {
  localStorage.removeItem(USERNAME_KEY);
}
