import axios from "axios";

const rawBase = (import.meta.env.VITE_API_URL as string | undefined) ?? "http://localhost:5180";
const base = rawBase.replace(/\/+$/, "");
const baseURL = base.endsWith("/api") ? base : `${base}/api`;

const api = axios.create({
  baseURL,
});

const JWT_STORAGE_KEY = "jwt";

// Injection automatique du JWT sur chaque requête
api.interceptors.request.use((config) => {
  const token = localStorage.getItem(JWT_STORAGE_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Session expirée / token invalide sur un endpoint privé (/files...) :
// on purge le token et on renvoie vers la page de connexion.
// Les 401 des endpoints publics (mot de passe de téléchargement incorrect)
// et de /auth/login sont laissés aux vues, qui affichent leur propre message.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error?.response?.status;
    const url = String(error?.config?.url ?? "");
    const isPrivateEndpoint = url.startsWith("/files");

    if (status === 401 && isPrivateEndpoint && localStorage.getItem(JWT_STORAGE_KEY)) {
      localStorage.removeItem(JWT_STORAGE_KEY);
      if (!window.location.pathname.startsWith("/login")) {
        window.location.assign("/login?expired=1");
      }
    }
    return Promise.reject(error);
  }
);

export default api;
