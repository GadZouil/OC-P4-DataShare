<template>
  <PublicLayout headerActionLabel="Se connecter" headerActionTo="/login">
    <div class="ds-card ds-upload-card">
      <h1 class="ds-title">Téléchargement</h1>

      <div v-if="error" class="ds-callout ds-callout--error">{{ error }}</div>

      <div v-if="meta" class="ds-download-meta">
        <div><strong>Fichier :</strong> {{ meta.fileName }}</div>
        <div><strong>Taille :</strong> {{ prettySize(meta.sizeBytes) }}</div>
        <div v-if="meta.expiresAt"><strong>Expire le :</strong> {{ meta.expiresAt }}</div>
      </div>

      <div v-if="meta?.requiresPassword" class="ds-field">
        <div class="ds-label">Mot de passe</div>
        <input
          class="ds-input"
          v-model="password"
          type="password"
          placeholder="Requis"
          autocomplete="off"
        />
      </div>

      <button class="ds-btn" :disabled="loading || !meta" @click="doDownload">
        Télécharger
      </button>
    </div>
  </PublicLayout>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import PublicLayout from "../layouts/PublicLayout.vue";
import { downloadFile, getFileMeta, type FileMeta } from "../api/files";

const route = useRoute();
const token = String(route.params.token ?? "");

const meta = ref<FileMeta | null>(null);
const password = ref("");
const loading = ref(false);
const error = ref<string | null>(null);

onMounted(async () => {
  error.value = null;
  try {
    meta.value = await getFileMeta(token);
  } catch (e: any) {
    error.value = "Lien invalide ou expiré.";
  }
});

function prettySize(bytes: number) {
  const units = ["o", "Ko", "Mo", "Go"];
  let n = bytes;
  let i = 0;
  while (n >= 1024 && i < units.length - 1) {
    n /= 1024;
    i++;
  }
  return `${n.toFixed(i === 0 ? 0 : 1)} ${units[i]}`;
}

async function doDownload() {
  if (!meta.value) return;

  error.value = null;
  loading.value = true;
  try {
    const blob = await downloadFile(token, password.value || undefined);

    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = meta.value.fileName || "download";
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  } catch (e: any) {
    error.value =
      e?.response?.status === 401
        ? "Mot de passe incorrect."
        : "Impossible de télécharger (lien invalide/expiré ?).";
  } finally {
    loading.value = false;
  }
}
</script>
