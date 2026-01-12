import { defineStore } from "pinia";
import { computed, ref } from "vue";

const KEY = "jwt";

export const useAuthStore = defineStore("auth", () => {
  const token = ref<string | null>(localStorage.getItem(KEY));

  const isLoggedIn = computed(() => !!token.value);

  function setToken(t: string | null) {
    token.value = t;
    if (t) localStorage.setItem(KEY, t);
    else localStorage.removeItem(KEY);
  }

  return { token, isLoggedIn, setToken };
});
