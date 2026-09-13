import api from "../services/api";
import type { AxiosError } from "axios";

export type UploadResult = {
  id: string;
  originalFileName: string;
  sizeBytes: number;
  contentType: string;
  createdAt: string;
  expiresAt: string;
  token: string;
  passwordRequired: boolean;
  tags?: string[];
  shareUrl: string;
};

export type PublicFileMeta = {
  id: string;
  originalFileName: string;
  sizeBytes: number;
  contentType: string;
  createdAt: string;
  expiresAt: string;
  passwordRequired: boolean;
};

export type MyFileItem = {
  id: string;
  originalFileName: string;
  sizeBytes: number;
  contentType: string;
  createdAt: string;
  expiresAt: string;
  token: string;
  passwordRequired: boolean;
  tags?: string[];
  shareUrl: string; // lien FRONT /download/:token
};

type ApiErrorBody = { message?: string; error?: string } | string | unknown[] | undefined;

// Messages renvoyés par l'API (anglais, techniques) -> messages utilisateur en français
const API_MESSAGES_FR: Record<string, string> = {
  "File is required.": "Aucun fichier sélectionné.",
  "File exceeds 1 GB.": "Le fichier dépasse la limite de 1 Go.",
  "ExpiresInDays must be between 1 and 7.": "La durée de conservation doit être comprise entre 1 et 7 jours.",
  "Forbidden file type.": "Type de fichier interdit : les exécutables (.exe, .bat, .cmd, .msi, .ps1…) ne sont pas acceptés.",
  "Password must be at least 6 characters.": "Mot de passe : minimum 6 caractères.",
  "Password required.": "Ce fichier est protégé : mot de passe requis.",
  "Invalid password.": "Mot de passe incorrect.",
  "Link expired.": "Ce lien a expiré : le fichier n'est plus disponible.",
};

function getApiErrorMessage(err: unknown, fallback: string): string {
  const e = err as AxiosError<ApiErrorBody>;

  // Pas de réponse HTTP du tout : API arrêtée ou réseau coupé
  if (e?.isAxiosError && !e.response) {
    return "Serveur injoignable. Vérifie que l'API est démarrée puis réessaie.";
  }

  if (e?.response?.status === 413) {
    return "Fichier trop volumineux pour le serveur (limite : 1 Go).";
  }

  const data = e?.response?.data;
  const body = typeof data === "object" && data !== null && !Array.isArray(data) ? data : undefined;
  const raw = typeof data === "string" ? data : body?.message || body?.error;
  const msg = typeof raw === "string" ? raw.trim() : "";

  if (msg.length > 0) return API_MESSAGES_FR[msg] ?? msg;
  return fallback;
}

function getStatus(err: unknown): number | undefined {
  return (err as AxiosError)?.response?.status;
}

/**
 * Upload (auth) -> POST /api/files
 */
export async function uploadFile(
  file: File,
  expiresInDays: number,
  password?: string,
  tags?: string[]
): Promise<UploadResult> {
  const form = new FormData();
  form.append("file", file);
  form.append("expiresInDays", String(expiresInDays));
  if (password && password.trim().length > 0) form.append("password", password.trim());
  if (tags?.length) tags.forEach((t) => form.append("tags", t));

  try {
    const endpoint = localStorage.getItem("jwt") ? "/files" : "/public/files";
    const res = await api.post(endpoint, form);

    const data = res.data as Omit<UploadResult, "shareUrl">;
    const shareUrl = new URL(`/download/${data.token}`, window.location.origin).toString();

    return { ...data, shareUrl };
  } catch (err) {
    throw new Error(getApiErrorMessage(err, "Téléversement impossible."));
  }
}

/**
 * Meta publique (sans auth) -> GET /api/public/files/{token}
 */
export async function getFileMeta(token: string): Promise<PublicFileMeta> {
  try {
    const res = await api.get(`/public/files/${encodeURIComponent(token)}`);
    return res.data as PublicFileMeta;
  } catch (err: unknown) {
    const status = getStatus(err);
    if (status === 404 || status === 410) throw new Error("Lien invalide ou expiré.");
    throw new Error(getApiErrorMessage(err, "Impossible de charger les informations du fichier."));
  }
}

/**
 * Download public -> POST /api/public/files/{token}/download
 */
export async function downloadFile(token: string, password?: string): Promise<void> {
  try {
    const res = await api.post(
      `/public/files/${encodeURIComponent(token)}/download`,
      { password: password?.trim() || null },
      { responseType: "blob" }
    );

    const cd = String(res.headers?.["content-disposition"] || "");
    const m = /filename\*=UTF-8''([^;]+)|filename="([^"]+)"/i.exec(cd);
    const raw = m?.[1] || m?.[2] || "download";
    const safeName = decodeURIComponent(raw);

    const blob = new Blob([res.data], {
      type: String(res.headers?.["content-type"] || "application/octet-stream"),
    });
    const url = URL.createObjectURL(blob);

    const a = document.createElement("a");
    a.href = url;
    a.download = safeName;
    document.body.appendChild(a);
    a.click();
    a.remove();

    URL.revokeObjectURL(url);
  } catch (err: unknown) {
    const status = getStatus(err);
    if (status === 401) throw new Error(getApiErrorMessage(err, "Mot de passe incorrect."));
    if (status === 404 || status === 410) throw new Error("Lien invalide ou expiré.");
    throw new Error(getApiErrorMessage(err, "Téléchargement impossible."));
  }
}

/**
 * Liste des fichiers de l'utilisateur connecté -> GET /api/files/me
 * status: all | active | expired
 */
export async function listMyFiles(status: "all" | "active" | "expired" = "active"): Promise<MyFileItem[]> {
  try {
    const res = await api.get("/files/me", { params: { status } });
    const arr = (res.data ?? []) as Array<Omit<MyFileItem, "shareUrl" | "tags"> & { tags?: string[] | null }>;

    return arr.map((d) => ({
      id: d.id,
      originalFileName: d.originalFileName,
      sizeBytes: d.sizeBytes,
      contentType: d.contentType,
      createdAt: d.createdAt,
      expiresAt: d.expiresAt,
      token: d.token,
      passwordRequired: !!d.passwordRequired,
      tags: d.tags ?? [],
      shareUrl: new URL(`/download/${d.token}`, window.location.origin).toString(),
    }));
  } catch (err) {
    throw new Error(getApiErrorMessage(err, "Impossible de charger vos fichiers."));
  }
}

/**
 * Supprimer un fichier (auth) -> DELETE /api/files/{id}
 */
export async function deleteMyFile(id: string): Promise<void> {
  try {
    await api.delete(`/files/${encodeURIComponent(id)}`);
  } catch (err) {
    throw new Error(getApiErrorMessage(err, "Suppression impossible."));
  }
}

export async function uploadPublicFile(
  file: File,
  expiresInDays: number,
  password?: string,
  tags?: string[]
): Promise<UploadResult> {
  const form = new FormData();
  form.append("file", file);
  form.append("expiresInDays", String(expiresInDays));
  if (password && password.trim().length > 0) form.append("password", password.trim());
  if (tags?.length) tags.forEach((t) => form.append("tags", t));

  try {
    const res = await api.post("/public/files", form);

    const data = res.data as Omit<UploadResult, "shareUrl">;

    const shareUrl = new URL(`/download/${data.token}`, window.location.origin).toString();
    return { ...data, shareUrl };
  } catch (err) {
    throw new Error(getApiErrorMessage(err, "Téléversement impossible."));
  }
}

