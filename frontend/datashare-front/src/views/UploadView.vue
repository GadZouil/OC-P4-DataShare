<template>
  <PublicLayout :headerActionLabel="headerLabel" :headerActionTo="headerTo">
    <!-- ÉTAT 1 : Home -->
    <div v-if="step === 'idle'" class="ds-upload-hero">
      <div class="ds-upload-question">Tu veux partager un<br />fichier ?</div>

      <button class="ds-upload-bigbtn" type="button" @click="pickFile">
        <span class="ds-upload-bigbtn-ring">
          <span class="ds-upload-bigbtn-core" aria-hidden="true">
            <!-- icône upload simple -->
            <svg width="30" height="30" viewBox="0 0 24 24" fill="none">
              <path
                d="M12 15V4m0 0 4 4M12 4 8 8"
                stroke="white"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
              <path
                d="M4 15v3a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-3"
                stroke="white"
                stroke-width="2"
                stroke-linecap="round"
              />
            </svg>
          </span>
        </span>
      </button>

      <input
        ref="fileInput"
        type="file"
        class="ds-hidden"
        @change="onFileSelected"
      />
    </div>

    <!-- ÉTAT 2 : Form -->
    <div v-else-if="step === 'form'" class="ds-upload-sheet">
      <div class="ds-card ds-upload-card">
        <h2 class="ds-title">Ajouter un fichier</h2>

        <div v-if="error" class="ds-callout ds-callout--error">{{ error }}</div>

        <div class="ds-file-row">
          <div class="ds-file-left">
            <span class="ds-file-icon">📄</span>
            <div class="ds-file-meta">
              <div class="ds-file-name">{{ file?.name }}</div>
              <div class="ds-file-size">{{ prettySize(file?.size ?? 0) }}</div>
            </div>
          </div>

          <button class="ds-file-change" type="button" @click="pickFile">Changer</button>
        </div>

        <div class="ds-field">
          <div class="ds-label">Mot de passe</div>
          <input
            class="ds-input"
            v-model="password"
            placeholder="Optionnel"
            type="password"
            autocomplete="off"
          />
          <div class="ds-hint">Si renseigné : min 6 caractères. :contentReference[oaicite:6]{index=6}</div>
        </div>

        <div class="ds-field">
          <div class="ds-label">Expiration</div>
          <select class="ds-select" v-model.number="expiresInDays">
            <option :value="1">Une journée</option>
            <option :value="2">Deux jours</option>
            <option :value="3">Trois jours</option>
            <option :value="4">Quatre jours</option>
            <option :value="5">Cinq jours</option>
            <option :value="6">Six jours</option>
            <option :value="7">Une semaine</option>
          </select>
          <div class="ds-hint">Max 7 jours. :contentReference[oaicite:7]{index=7}</div>
        </div>

        <button class="ds-btn" :disabled="loading" @click="doUpload">
          Téléverser
        </button>
      </div>
    </div>

    <!-- ÉTAT 3 : Success -->
    <div v-else class="ds-upload-sheet">
      <div class="ds-card ds-upload-card">
        <h2 class="ds-title">Ajouter un fichier</h2>

        <div class="ds-file-row">
          <div class="ds-file-left">
            <span class="ds-file-icon">📄</span>
            <div class="ds-file-meta">
              <div class="ds-file-name">{{ file?.name }}</div>
              <div class="ds-file-size">{{ prettySize(file?.size ?? 0) }}</div>
            </div>
          </div>

          <button class="ds-file-change" type="button" @click="reset">
            Changer
          </button>
        </div>

        <div class="ds-success-text">
          Félicitations, ton fichier sera conservé chez nous pendant une semaine !
        </div>

        <div class="ds-linkbox">
          <a class="ds-linkbox-url" :href="shareUrl" target="_blank" rel="noreferrer">
            {{ shareUrl }}
          </a>
        </div>

        <button class="ds-btn ds-btn--secondary" type="button" @click="copyLink">
          Copier le lien
        </button>
      </div>
    </div>
  </PublicLayout>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import PublicLayout from "../layouts/PublicLayout.vue";
import { uploadFile } from "../api/files";

const fileInput = ref<HTMLInputElement | null>(null);

type Step = "idle" | "form" | "success";
const step = ref<Step>("idle");

const file = ref<File | null>(null);
const password = ref("");
const expiresInDays = ref(7); // si tu veux coller au Figma plutôt, mets 1 ici
const shareUrl = ref("");

const loading = ref(false);
const error = ref<string | null>(null);

const isLoggedIn = computed(() => !!localStorage.getItem("ds_token"));
const headerLabel = computed(() => (isLoggedIn.value ? "Mon espace" : "Se connecter"));
const headerTo = computed(() => (isLoggedIn.value ? "/me" : "/login"));

function pickFile() {
  error.value = null;
  fileInput.value?.click();
}

function onFileSelected(e: Event) {
  const input = e.target as HTMLInputElement;
  const f = input.files?.[0] ?? null;

  if (!f) return;

  // 1 Go max (front), le back validera aussi
  const oneGb = 1_073_741_824;
  if (f.size > oneGb) {
    error.value = "La taille du fichier est limitée à 1 Go.";
    input.value = "";
    return;
  }

  file.value = f;
  step.value = "form";
}

function reset() {
  file.value = null;
  password.value = "";
  expiresInDays.value = 7;
  shareUrl.value = "";
  error.value = null;
  step.value = "idle";
  if (fileInput.value) fileInput.value.value = "";
}

function prettySize(bytes: number) {
  if (!bytes) return "0 o";
  const units = ["o", "Ko", "Mo", "Go"];
  let n = bytes;
  let i = 0;
  while (n >= 1024 && i < units.length - 1) {
    n /= 1024;
    i++;
  }
  return `${n.toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
}

async function doUpload() {
  error.value = null;
  if (!file.value) return;

  if (password.value && password.value.length < 6) {
    error.value = "Mot de passe fichier : minimum 6 caractères.";
    return;
  }

  loading.value = true;
  try {
    const res = await uploadFile({
      file: file.value,
      password: password.value || undefined,
      expiresInDays: expiresInDays.value,
    });

    // on accepte plusieurs formats de réponse (token / url)
    const token = res.token ?? "";
    const url = res.url ?? (token ? `${window.location.origin}/download/${token}` : "");

    if (!url) {
      throw new Error("Réponse upload invalide : pas de lien retourné.");
    }

    shareUrl.value = url;
    step.value = "success";
  } catch (e: any) {
    const msg =
      e?.response?.data?.message ||
      e?.message ||
      "Erreur lors du téléversement.";
    error.value = String(msg);
  } finally {
    loading.value = false;
  }
}

async function copyLink() {
  try {
    await navigator.clipboard.writeText(shareUrl.value);
  } catch {
    // fallback simple
    window.prompt("Copie ce lien :", shareUrl.value);
  }
}
</script>
