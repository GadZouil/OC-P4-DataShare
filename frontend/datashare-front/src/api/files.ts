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

function getApiErrorMessage(err: unknown, fallback: string): string {
  const e = err as AxiosError<any>;
  const msg =
    e?.response?.data?.message ||
    (Array.isArray(e?.response?.data) ? undefined : e?.response?.data?.error) ||
    e?.message;

  return (typeof msg === "string" && msg.trim().length > 0) ? msg : fallback;
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
    const res = await api.post("/files", form, {
      headers: { "Content-Type": "multipart/form-data" },
    });

    const data = res.data as Omit<UploadResult, "shareUrl">;

    // Lien FRONT (route /download/:token)
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
    const d = res.data as any;

    // Normalisation: certains back renvoient fileName, d'autres originalFileName, etc.
    const fileName =
      d.originalFileName ??
      d.fileName ??
      d.name ??
      d.filename ??
      "download";

    return {
      ...d,
      fileName,
    } as PublicFileMeta;
  } catch (err: any) {
    const status = err?.response?.status;

    // 404 / 410 = lien invalide / expiré
    if (status === 404 || status === 410) {
      throw new Error("Lien invalide ou expiré.");
    }

    throw new Error(
      getApiErrorMessage(err, "Impossible de charger les informations du fichier.")
    );
  }
}

/**
 * Download public -> POST /api/public/files/{token}/download
 * (backend renvoie directement le fichier en File(stream, contentType, originalName))
 */
export async function downloadFile(token: string, password?: string): Promise<void> {
  try {
    const res = await api.post(
      `/public/files/${encodeURIComponent(token)}/download`,
      { password: password?.trim() || null },
      { responseType: "blob" }
    );

    // Récup du nom de fichier (Content-Disposition)
    const cd = String(res.headers?.["content-disposition"] || "");
    const m = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(cd);
    const filename = m?.[1] ?? m?.[2] ?? "download";


    const safeName = decodeURIComponent(filename);

    const blob = new Blob([res.data], { type: res.headers?.["content-type"] || "application/octet-stream" });
    const url = URL.createObjectURL(blob);

    const a = document.createElement("a");
    a.href = url;
    a.download = safeName;
    document.body.appendChild(a);
    a.click();
    a.remove();

    URL.revokeObjectURL(url);
  } catch (err: any) {
    const status = err?.response?.status;

    if (status === 401) {
      // Cas password requis / invalid
      throw new Error(err?.response?.data?.message || "Mot de passe incorrect.");
    }
    if (status === 404 || status === 410) {
      throw new Error("Lien invalide ou expiré.");
    }

    throw new Error(getApiErrorMessage(err, "Téléchargement impossible."));
  }
}
