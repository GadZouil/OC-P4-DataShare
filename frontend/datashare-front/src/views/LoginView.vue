<template>
  <PublicLayout headerActionLabel="Se connecter" headerActionTo="/login">
    <div class="ds-card">
      <h1 class="ds-title">Connexion</h1>

      <div v-if="error" class="ds-callout ds-callout--error" style="white-space: pre-line;">
        {{ error }}
      </div>
      <div v-else-if="sessionExpired" class="ds-callout">
        Ta session a expiré, reconnecte-toi.
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
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import PublicLayout from "../layouts/PublicLayout.vue";
import { login, setUsername } from "../api/auth";

const router = useRouter();
const route = useRoute();

// Affiché lorsque l'intercepteur API a redirigé ici après un 401 (token expiré)
const sessionExpired = computed(() => route.query.expired === "1");

const email = ref("");
const password = ref("");
const error = ref<string | null>(null);
const loading = ref(false);

function extractApiError(e: unknown): string {
  // login() (api/auth.ts) a déjà traduit l'erreur HTTP en message lisible
  const msg = (e as Error)?.message;
  return typeof msg === "string" && msg.trim().length > 0 ? msg : "Erreur.";
}

async function onSubmit() {
  error.value = null;
  loading.value = true;

  try {
    await login(email.value, password.value);
    setUsername(email.value);

    // Retour vers la page demandée avant la redirection (ex. /me), sinon l'accueil (upload)
    const redirect = typeof route.query.redirect === "string" ? route.query.redirect : "/";
    router.push(redirect.startsWith("/") ? redirect : "/");
  } catch (e: unknown) {
    error.value = extractApiError(e);
  } finally {
    loading.value = false;
  }
}
</script>
