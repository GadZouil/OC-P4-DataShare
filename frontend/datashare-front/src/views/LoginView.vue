<template>
  <PublicLayout headerActionLabel="Se connecter" headerActionTo="/login">
    <div class="ds-card">
      <h1 class="ds-title">Connexion</h1>

      <div v-if="error" class="ds-callout" style="white-space: pre-line;">
        {{ error }}
      </div>

      <form class="ds-form" @submit.prevent="onSubmit">
        <div class="ds-field">
          <div class="ds-label">Email</div>
          <input
            class="ds-input"
            v-model.trim="email"
            placeholder="Saisissez votre email…"
            type="email"
            inputmode="email"
            autocomplete="email"
            required
          />
        </div>

        <div class="ds-field">
          <div class="ds-label">Mot de passe</div>
          <input
            class="ds-input"
            v-model="password"
            placeholder="Saisissez votre mot de passe…"
            type="password"
            autocomplete="current-password"
            required
          />
        </div>

        <RouterLink class="ds-link" to="/register">Créer un compte</RouterLink>

        <button class="ds-btn" :disabled="loading">
          {{ loading ? "Connexion..." : "Connexion" }}
        </button>
      </form>
    </div>
  </PublicLayout>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import PublicLayout from "../layouts/PublicLayout.vue";
import { login } from "../api/auth";

const router = useRouter();

const email = ref("");
const password = ref("");
const error = ref<string | null>(null);
const loading = ref(false);

function extractApiError(e: any): string {
  const status = e?.response?.status;
  const data = e?.response?.data;

  if (status === 401) return "Identifiants incorrects.";

  // si le back renvoie { message: "..." }
  if (data?.message && typeof data.message === "string") return data.message;

  // si le back renvoie du texte brut
  if (typeof data === "string") return data;

  // si axios met un message standard
  if (e?.message) return e.message;

  return "Erreur.";
}

async function onSubmit() {
  error.value = null;
  loading.value = true;

  try {
    await login(email.value, password.value);

    // Choisis ta page post-login (à adapter selon tes routes)
    router.push("/");
  } catch (e: any) {
    error.value = extractApiError(e);
  } finally {
    loading.value = false;
  }
}
</script>
