<template>
  <PublicLayout :headerActionLabel="headerLabel" :headerActionTo="headerTo">
    <div v-if="step === 'idle'" class="ds-upload-hero">
      <div class="ds-upload-question">Tu veux partager un fichier ?</div>

      <button class="ds-upload-bigbtn" type="button" @click="pickFile">
        <div class="ds-upload-bigbtn-ring">
          <div class="ds-upload-bigbtn-core">
            <svg width="26" height="26" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path
                d="M12 16V4m0 0l-4 4m4-4l4 4M4 16v3a1 1 0 001 1h14a1 1 0 001-1v-3"
                stroke="white"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          </div>
        </div>
      </button>

      <input ref="fileInput" class="ds-hidden" type="file" @change="onFileSelected" />
    </div>

    <div v-else class="ds-upload-sheet">
      <div class="ds-card ds-upload-card">
        <h1 class="ds-title">Ajouter un fichier</h1>

        <div v-if="error" class="ds-callout ds-callout--error">{{ error }}</div>

        <div v-if="file" class="ds-file-row">
          <div class="ds-file-left">
            <div aria-hidden="true">📄</div>
            <div class="ds-file-meta">
              <div class="ds-file-name">{{ file.name }}</div>
              <div class="ds-file-size">{{ prettySize(file.size) }}</div>
            </div>
          </div>

          <button
            v-if="step === 'form'"
            class="ds-file-change"
            type="button"
            @click="pickFile"
          >
            Changer
          </button>
        </div>

        <div v-if="step === 'form'">
          <div class="ds-field">
            <div class="ds-label">Mot de passe</div>
            <input
              class="ds-input"
              v-model="password"
              placeholder="Optionnel"
              type="password"
              autocomplete="new-password"
            />
            <div class="ds-hint">Si renseigné : min 6 caractères.</div>
          </div>

          <div class="ds-field">
            <div class="ds-label">Expiration</div>
            <select class="ds-input" v-model.number="expiresInDays">
              <option v-for="d in 7" :key="d" :value="d">
                {{ d === 1 ? "1 jour" : `${d} jours` }}
              </option>
            </select>
            <div class="ds-hint">Max 7 jours.</div>
          </div>

          <div class="ds-field">
            <div class="ds-label">Tags (optionnel)</div>
            <div class="ds-tag-input-wrapper">
              <input
                class="ds-tag-input"
                v-model="tagInput"
                placeholder="Ajouter un tag (Enter pour ajouter)"
                @keydown.enter="addTag"
              />
            </div>
            <div v-if="tagError" class="ds-callout ds-callout--error">
              {{ tagError }}
            </div>
            <div v-if="tags.length > 0" class="ds-tags">
              <div
                v-for="(tag, idx) in tags"
                :key="idx"
                class="ds-chip"
              >
                <span>{{ tag }}</span>
                <button
                  class="ds-chip-remove"
                  type="button"
                  @click="removeTag(idx)"
                  :aria-label="`Supprimer ${tag}`"
                >
                  ✕
                </button>
              </div>
            </div>
            <div class="ds-hint">Max 24 caractères par tag, pas de doublons.</div>
          </div>

          <button class="ds-btn" type="button" :disabled="loading || !file" @click="doUpload">
            {{ loading ? "Téléversement..." : "Téléverser" }}
          </button>
        </div>

        <div v-else-if="step === 'done'" class="ds-success">
          <div class="ds-success-text">
            Félicitations, ton fichier sera conservé chez nous pendant {{ expiresInDays }} jour<span
              v-if="expiresInDays > 1"
              >s</span
            >
            !
          </div>

          <div class="ds-linkbox">
            <a class="ds-linkbox-url" :href="shareUrl" target="_blank" rel="noreferrer">
              {{ shareUrl }}
            </a>
          </div>

          <button class="ds-btn" type="button" @click="copyLink">
            Copier le lien
          </button>
        </div>
      </div>
    </div>
  </PublicLayout>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import PublicLayout from "../layouts/PublicLayout.vue";
import { uploadFile, uploadPublicFile } from "../api/files";
import { isAuthenticated } from "../api/auth";

type Step = "idle" | "form" | "done";

const fileInput = ref<HTMLInputElement | null>(null);
const router = useRouter();

const step = ref<Step>("idle");
const file = ref<File | null>(null);

const password = ref("");
const expiresInDays = ref<number>(7);
const tags = ref<string[]>([]);
const tagError = ref<string | null>(null);
const tagInput = ref("");

const loading = ref(false);
const error = ref<string | null>(null);
const shareUrl = ref("");

const isLoggedIn = computed(() => isAuthenticated());
const headerLabel = computed(() => (isLoggedIn.value ? "Mon Espace" : "Se connecter"));
const headerTo = computed(() => (isLoggedIn.value ? "/me" : "/login"));

function pickFile() {
  error.value = null;

  // if (!isLoggedIn.value) {
  //   router.push("/login");
  //   return;
  // }

  fileInput.value?.click();
}

function onFileSelected(e: Event) {
  const target = e.target as HTMLInputElement;
  const f = target.files?.[0] ?? null;
  if (!f) return;

  file.value = f;
  step.value = "form";
  shareUrl.value = "";
}

function prettySize(bytes: number): string {
  if (bytes < 1024) return `${bytes} o`;
  const kb = bytes / 1024;
  if (kb < 1024) return `${kb.toFixed(1)} Ko`;
  const mb = kb / 1024;
  if (mb < 1024) return `${mb.toFixed(1)} Mo`;
  const gb = mb / 1024;
  return `${gb.toFixed(2)} Go`;
}

async function doUpload() {
  error.value = null;

  // if (!isLoggedIn.value) {
  //   router.push("/login");
  //   return;
  // }

  if (!file.value) return;

  loading.value = true;
  try {
    const res = isAuthenticated()
      ? await uploadFile(file.value, expiresInDays.value, password.value, tags.value)
      : await uploadPublicFile(file.value, expiresInDays.value, password.value, tags.value);


    if (!res.shareUrl) {
      throw new Error("Réponse upload invalide : pas de lien retourné.");
    }

    shareUrl.value = res.shareUrl;
    step.value = "done";
  } catch (e: any) {
    error.value = String(e?.message || "Erreur lors du téléversement.");
  } finally {
    loading.value = false;
  }
}

async function copyLink() {
  if (!shareUrl.value) return;
  try {
    await navigator.clipboard.writeText(shareUrl.value);
  } catch {
    window.prompt("Copie le lien :", shareUrl.value);
  }
}

function normalizeTag(tag: string): string {
  return tag.trim().toLowerCase();
}

function addTag() {
  tagError.value = null;

  const t = tagInput.value.trim();

  if (!t) {
    tagError.value = "Le tag ne peut pas être vide.";
    return;
  }

  if (t.length > 24) {
    tagError.value = "Tag trop long (24 caractères max).";
    return;
  }

  const normalized = t.toLowerCase();
  const exists = tags.value.some((x) => x.toLowerCase() === normalized);

  if (exists) {
    tagError.value = "Ce tag est déjà présent.";
    return;
  }

  tags.value.push(t);
  tagInput.value = "";
}

function removeTag(idx: number) {
  tags.value.splice(idx, 1);
  tagError.value = null;
}
</script>
