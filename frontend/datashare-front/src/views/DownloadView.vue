<template>
  <PublicLayout headerActionLabel="Se connecter" headerActionTo="/login">
    <div class="ds-card ds-upload-card">
      <h1 class="ds-title">Téléchargement</h1>

      <div v-if="error" class="ds-callout ds-callout--error">
        {{ error }}
      </div>

      <template v-if="meta">
        <div class="ds-file-row">
          <div class="ds-file-left">
            <div class="ds-file-ico" aria-hidden="true">
              <svg width="18" height="22" viewBox="0 0 18 22" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M3 1H10.5L16 6.5V21H3V1Z" stroke="black" stroke-opacity="0.5"/>
                <path d="M10.5 1V6.5H16" stroke="black" stroke-opacity="0.5"/>
              </svg>
            </div>

            <div class="ds-file-meta">
              <div class="ds-file-name">{{ meta.originalFileName }}</div>
              <div class="ds-file-sub">
                {{ formatBytes(meta.sizeBytes) }} · {{ meta.contentType }} · Expire le {{ formatDate(meta.expiresAt) }}
              </div>
            </div>
          </div>
        </div>

        <div v-if="meta.passwordRequired" class="ds-field">
          <div class="ds-label">Mot de passe</div>
          <input
            class="ds-input"
            v-model="password"
            placeholder="Requis"
            type="password"
            autocomplete="current-password"
          />
          <div class="ds-hint">Ce fichier est protégé par un mot de passe.</div>
        </div>

        <button class="ds-btn" :disabled="loading" @click="doDownload">
          Télécharger
        </button>
      </template>

      <button v-else class="ds-btn" disabled>
        Télécharger
      </button>
    </div>
  </PublicLayout>
</template>

<script setup lang="ts">
import { ref, watchEffect } from "vue";
import { useRoute } from "vue-router";
import PublicLayout from "../layouts/PublicLayout.vue";
import { getFileMeta, downloadFile, type PublicFileMeta } from "../api/files";

const route = useRoute();

const meta = ref<PublicFileMeta | null>(null);
const password = ref("");
const error = ref<string | null>(null);
const loading = ref(false);

function formatBytes(bytes: number) {
  const units = ["o", "Ko", "Mo", "Go", "To"];
  let value = bytes;
  let i = 0;
  while (value >= 1024 && i < units.length - 1) {
    value /= 1024;
    i++;
  }
  return `${value.toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
}

function formatDate(iso: string) {
  try {
    const d = new Date(iso);
    return d.toLocaleDateString("fr-FR", { year: "numeric", month: "2-digit", day: "2-digit" });
  } catch {
    return iso;
  }
}

async function loadMeta(token: string) {
  error.value = null;
  meta.value = null;
  try {
    meta.value = await getFileMeta(token);
  } catch (e: any) {
    error.value = e?.message ?? "Lien invalide ou expiré.";
  }
}

async function doDownload() {
  const token = String(route.params.token || "");
  if (!token) {
    error.value = "Lien invalide ou expiré.";
    return;
  }

  if (meta.value?.passwordRequired && !password.value.trim()) {
    error.value = "Mot de passe requis.";
    return;
  }

  loading.value = true;
  error.value = null;
  try {
    await downloadFile(token, password.value || undefined);
  } catch (e: any) {
    error.value = e?.message ?? "Téléchargement impossible.";
  } finally {
    loading.value = false;
  }
}

watchEffect(() => {
  const token = String(route.params.token || "");
  if (token) loadMeta(token);
});
</script>
