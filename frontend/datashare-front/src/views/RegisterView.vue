<template>
  <PublicLayout headerActionLabel="Se connecter" headerActionTo="/login">
    <div class="ds-card">
      <h1 class="ds-title">Créer un compte</h1>

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
            v-model="pwd"
            placeholder="Saisissez votre mot de passe…"
            type="password"
            autocomplete="new-password"
            required
          />
        </div>

        <div class="ds-field">
          <div class="ds-label">Verification du mot de passe</div>
          <input
            class="ds-input"
            v-model="pwd2"
            placeholder="Saisissez le à nouveau…"
            type="password"
            autocomplete="new-password"
            required
          />
        </div>

        <RouterLink class="ds-link" to="/login">J'ai déjà un compte</RouterLink>

        <button class="ds-btn" :disabled="loading">
          {{ loading ? "Création..." : "Créer mon compte" }}
        </button>
      </form>
    </div>
  </PublicLayout>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import PublicLayout from "../layouts/PublicLayout.vue";
import { register } from "../api/auth";

const router = useRouter();

const email = ref("");
const pwd = ref("");
const pwd2 = ref("");
const error = ref<string | null>(null);
const loading = ref(false);

function extractApiError(e: any): string {
  const data = e?.response?.data;

  // Identity renvoie souvent: [{ code, description }, ...]
  if (Array.isArray(data) && data.length && data[0]?.description) {
    return data.map((x: any) => x.description).join("\n");
  }

  if (data?.message && typeof data.message === "string") return data.message;
  if (typeof data === "string") return data;
  if (e?.message) return e.message;

  return "Erreur.";
}

function validatePasswordFront(p: string): string[] {
  const errors: string[] = [];

  // Identity par défaut = 6 minimum
  if (p.length < 6) errors.push("Mot de passe : minimum 6 caractères.");
  if (!/[A-Z]/.test(p)) errors.push("Mot de passe : au moins 1 majuscule.");
  if (!/[0-9]/.test(p)) errors.push("Mot de passe : au moins 1 chiffre.");
  if (!/[^a-zA-Z0-9]/.test(p)) errors.push("Mot de passe : au moins 1 caractère spécial.");

  return errors;
}

async function onSubmit() {
  error.value = null;

  const pwdErrors = validatePasswordFront(pwd.value);
  if (pwdErrors.length) {
    error.value = pwdErrors.join("\n");
    return;
  }

  if (pwd.value !== pwd2.value) {
    error.value = "Les mots de passe ne correspondent pas.";
    return;
  }

  loading.value = true;
  try {
    await register(email.value, pwd.value);
    router.push("/login");
  } catch (e: any) {
    error.value = extractApiError(e);
  } finally {
    loading.value = false;
  }
}
</script>
