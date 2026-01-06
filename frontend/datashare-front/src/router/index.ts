import { createRouter, createWebHistory } from "vue-router";
import LoginView from "../views/LoginView.vue";
import RegisterView from "../views/RegisterView.vue";

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
    { path: "/login", component: LoginView },
    { path: "/register", component: RegisterView },
  ],
});

export default router;
