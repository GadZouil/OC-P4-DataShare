<template>
  <PublicLayout headerActionLabel="Se connecter" headerActionTo="/login">
    <div class="ds-card">
      <h1 class="ds-title">Créer un compte</h1>

      <div v-if="error" class="ds-callout ds-callout--error">{{ error }}</div>

      <form class="ds-form" @submit.prevent="onSubmit">
        <div class="ds-field">
          <div class="ds-label">Email</div>
          <input
            class="ds-input"
            v-model.trim="email"
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
          <div class="ds-hint">
            8 caractères min, 1 majuscule, 1 chiffre, 1 caractère spécial.
          </div>
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

type IdentityError = { code?: string; description?: string };

const router = useRouter();

const email = ref("");
const pwd = ref("");
const pwd2 = ref("");
const error = ref<string | null>(null);
const loading = ref(false);

function mapIdentityCode(code?: string) {
  switch (code) {
    // Email / user
    case "DuplicateUserName":
    case "DuplicateEmail":
      return "Cet email est déjà utilisé.";
    case "InvalidEmail":
      return "Email invalide.";
    case "InvalidUserName":
      return "Nom d’utilisateur invalide.";

    // Password rules (ASP.NET Identity)
    case "PasswordTooShort":
      return "Mot de passe trop court (8 caractères minimum).";
    case "PasswordRequiresNonAlphanumeric":
      return "Le mot de passe doit contenir au moins un caractère spécial (ex: !, ?, #, @…).";
    case "PasswordRequiresDigit":
      return "Le mot de passe doit contenir au moins un chiffre.";
    case "PasswordRequiresUpper":
      return "Le mot de passe doit contenir au moins une majuscule.";
    case "PasswordRequiresLower":
      return "Le mot de passe doit contenir au moins une minuscule.";

    // Other common Identity errors
    case "PasswordMismatch":
      return "Mot de passe incorrect.";
    case "ConcurrencyFailure":
      return "Conflit de mise à jour. Réessaie.";
    default:
      return null;
  }
}

function normalizeRegisterError(e: any): string {
  const data = e?.response?.data;

  // format : [{code, description}, ...]
  if (Array.isArray(data)) {
    const messages = data
      .map((x: IdentityError) => mapIdentityCode(x.code) ?? x.description ?? null)
      .filter(Boolean) as string[];

    if (messages.length) return messages.join(" ");
    return "Erreur lors de la création du compte.";
  }

  // format : { message: "..." } ou string
  if (typeof data === "string") return data;
  if (data?.message) return String(data.message);

  // axios fallback
  if (e?.message) return String(e.message);

  return "Erreur lors de la création du compte.";
}

async function onSubmit() {
  error.value = null;

  // validation front minimale (le back est la source de vérité)
  if (!email.value.includes("@")) {
    error.value = "Email invalide.";
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
    error.value = normalizeRegisterError(e);
  } finally {
    loading.value = false;
  }
}
</script>
