import axios from "axios";

const rawBase = (import.meta.env.VITE_API_URL as string | undefined) ?? "http://localhost:5180";
const base = rawBase.replace(/\/+$/, "");
const baseURL = base.endsWith("/api") ? base : `${base}/api`;

const api = axios.create({
  baseURL,
});

const JWT_STORAGE_KEY = "jwt";

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(JWT_STORAGE_KEY);
  if (token) {
    config.headers = config.headers ?? {};
    (config.headers as any).Authorization = `Bearer ${token}`;
  }
  return config;
});

export default api;
