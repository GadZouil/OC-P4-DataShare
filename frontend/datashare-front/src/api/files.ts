import api from "../services/api";

export type UploadResponse = {
  token?: string;
  url?: string;
  expiresAt?: string;
};

export type FileMeta = {
  fileName: string;
  sizeBytes: number;
  expiresAt?: string;
  requiresPassword?: boolean;
};

export async function uploadFile(params: {
  file: File;
  password?: string;
  expiresInDays?: number;
}): Promise<UploadResponse> {
  const fd = new FormData();
  fd.append("file", params.file);

  if (params.password) fd.append("password", params.password);
  if (params.expiresInDays) fd.append("expiresInDays", String(params.expiresInDays));

  const res = await api.post("/files", fd);
  return res.data;
}

export async function getFileMeta(token: string): Promise<FileMeta> {
  const res = await api.get(`/files/${encodeURIComponent(token)}/meta`);
  return res.data;
}

export async function downloadFile(token: string, password?: string): Promise<Blob> {
  // Variante “POST” pratique si mot de passe à transmettre (à adapter à ton back)
  const res = await api.post(
    `/files/${encodeURIComponent(token)}/download`,
    { password },
    { responseType: "blob" }
  );
  return res.data as Blob;
}
