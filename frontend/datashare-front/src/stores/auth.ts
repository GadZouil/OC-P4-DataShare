import { defineStore } from "pinia";

export const useAuthStore = defineStore("auth", {
  state: () => ({
    token: localStorage.getItem("ds_token") as string | null,
  }),
  getters: {
    isAuthenticated: (s) => !!s.token,
  },
  actions: {
    setToken(token: string) {
      this.token = token;
      localStorage.setItem("ds_token", token);
    },
    logout() {
      this.token = null;
      localStorage.removeItem("ds_token");
    },
  },
});
