import axios from "axios";

const apiBase = (import.meta.env.VITE_API_URL as string | undefined) ?? "http://localhost:5180";

const api = axios.create({
  baseURL: `${apiBase}/api`,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("ds_token");
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default api;
