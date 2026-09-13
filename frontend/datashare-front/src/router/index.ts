import { createRouter, createWebHistory } from "vue-router";
import LoginView from "../views/LoginView.vue";
import RegisterView from "../views/RegisterView.vue";
import { isAuthenticated } from "../api/auth";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      name: "upload",
      component: () => import("../views/UploadView.vue"),
    },
    {
      path: "/download/:token",
      name: "download",
      component: () => import("../views/DownloadView.vue"),
    },
    { path: "/login", name: "login", component: LoginView },
    { path: "/register", name: "register", component: RegisterView },
    {
      path: "/me",
      name: "me",
      component: () => import("../views/MeView.vue"),
      meta: { requiresAuth: true },
    },
    // Toute route inconnue renvoie vers la page d'accueil (upload)
    { path: "/:pathMatch(.*)*", redirect: "/" },
  ],
});

// Garde de navigation : les pages privées exigent un JWT en local storage.
// (Le backend reste la seule autorité : un token invalide produira un 401,
// intercepté par services/api.ts qui renvoie vers /login.)
router.beforeEach((to) => {
  if (to.meta.requiresAuth && !isAuthenticated()) {
    return { path: "/login", query: { redirect: to.fullPath } };
  }
  return true;
});

export default router;
