<template>
  <PublicLayout headerActionLabel="Se connecter" headerActionTo="/login">
    <div class="ds-card">
      <h1 class="ds-title">Créer un compte</h1>

      <div v-if="errors.length" class="ds-callout ds-callout--error">
        <ul class="ds-errors">
          <li v-for="(m, i) in errors" :key="i">{{ m }}</li>
        </ul>
      </div>

      <form class="ds-form" @submit.prevent="onSubmit">
        <div class="ds-field">
          <div class="ds-label">Email</div>
          <input
            class="ds-input"
            v-model="email"
            placeholder="Saisissez votre email…"
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
          Créer mon compte
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
const loading = ref(false);
const errors = ref<string[]>([]);

function setErrorsFromError(e: unknown) {
  const msg = (e as any)?.message ?? "Erreur.";
  errors.value = String(msg).split("\n").filter(Boolean);
}

async function onSubmit() {
  errors.value = [];

  // Specs: mdp min 8 caractères (pas “maj+chiffre+spécial” obligé)
  if (pwd.value.length < 8) {
    errors.value = ["Mot de passe : minimum 8 caractères."];
    return;
  }
  if (pwd.value !== pwd2.value) {
    errors.value = ["Les mots de passe ne correspondent pas."];
    return;
  }

  loading.value = true;
  try {
    await register(email.value, pwd.value);
    router.push("/login");
  } catch (e) {
    setErrorsFromError(e);
  } finally {
    loading.value = false;
  }
}
</script>
