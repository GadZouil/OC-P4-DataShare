<template>
  <div class="ds-me-page">
    <aside class="ds-me-sidebar">
      <div class="ds-me-brand">DataShare</div>

      <nav class="ds-me-nav">
        <button class="ds-me-navitem is-active" type="button">Mes fichiers</button>
      </nav>

      <div class="ds-me-sidefooter">Copyright DataShare© 2025</div>
    </aside>

    <div class="ds-me-main">
      <header class="ds-me-topbar">
        <button class="ds-me-btn-dark" type="button" @click="goUpload">
          Ajouter des fichiers
        </button>

        <button class="ds-me-logout" type="button" @click="onLogout">
          <span class="ds-me-logout-ic" aria-hidden="true">↪</span>
          Déconnexion
        </button>
      </header>

      <header class="ds-me-mobilebar">
        <button class="ds-me-burger" type="button" @click="drawerOpen = true" aria-label="Ouvrir le menu">
          <span aria-hidden="true">☰</span>
        </button>

        <div class="ds-me-user">
          <div class="ds-me-avatar" aria-hidden="true">{{ userInitial }}</div>
          <div class="ds-me-username">{{ username }}</div>
        </div>
      </header>

      <main class="ds-me-content">
        <h1 class="ds-me-title">Mes fichiers</h1>

        <div class="ds-me-tabs" role="tablist" aria-label="Filtres">
          <button class="ds-me-tab" :class="{ 'is-active': tab === 'all' }" type="button" @click="tab = 'all'">
            Tous
          </button>
          <button class="ds-me-tab" :class="{ 'is-active': tab === 'active' }" type="button" @click="tab = 'active'">
            Actifs
          </button>
          <button class="ds-me-tab" :class="{ 'is-active': tab === 'expired' }" type="button" @click="tab = 'expired'">
            Expiré
          </button>
        </div>

        <div v-if="error" class="ds-callout ds-callout--error">{{ error }}</div>

        <div class="ds-me-list" v-if="!loading">
          <div
            v-for="f in filteredFiles"
            :key="f.id"
            class="ds-me-row"
            :class="{ 'is-expired': isExpired(f) }"
          >
            <div class="ds-me-row-left">
              <div class="ds-me-fileic" aria-hidden="true">
                <svg v-if="fileKind(f) === 'image'" viewBox="0 0 24 24">
                  <path d="M6 2h9l3 3v17H6z" fill="none" stroke="currentColor" stroke-width="2"/>
                  <path d="M15 2v5h5" fill="none" stroke="currentColor" stroke-width="2"/>
                  <path d="M8 16l3-3 2 2 3-4 2 3" fill="none" stroke="currentColor" stroke-width="2"/>
                </svg>
                <svg v-else-if="fileKind(f) === 'audio'" viewBox="0 0 24 24">
                  <path d="M6 2h9l3 3v17H6z" fill="none" stroke="currentColor" stroke-width="2"/>
                  <path d="M15 2v5h5" fill="none" stroke="currentColor" stroke-width="2"/>
                  <path d="M10 18a2 2 0 1 0 0-4 2 2 0 0 0 0 4z" fill="none" stroke="currentColor" stroke-width="2"/>
                  <path d="M12 14V9l6-1v5" fill="none" stroke="currentColor" stroke-width="2"/>
                </svg>
                <svg v-else-if="fileKind(f) === 'video'" viewBox="0 0 24 24">
                  <path d="M6 2h9l3 3v17H6z" fill="none" stroke="currentColor" stroke-width="2"/>
                  <path d="M15 2v5h5" fill="none" stroke="currentColor" stroke-width="2"/>
                  <path d="M10 10l7 4-7 4z" fill="none" stroke="currentColor" stroke-width="2"/>
                </svg>
                <svg v-else viewBox="0 0 24 24">
                  <path d="M6 2h9l3 3v17H6z" fill="none" stroke="currentColor" stroke-width="2"/>
                  <path d="M15 2v5h5" fill="none" stroke="currentColor" stroke-width="2"/>
                </svg>
              </div>

              <div class="ds-me-meta">
                <div class="ds-me-name" :title="f.originalFileName">{{ f.originalFileName }}</div>
                <div class="ds-me-sub" :class="{ 'is-expired': isExpired(f) }">
                  {{ expiryLabel(f) }}
                </div>
              </div>
            </div>

            <div class="ds-me-row-right">
              <div v-if="isExpired(f)" class="ds-me-expired-note">
                Ce fichier à expiré, il n’est plus stocké chez nous
              </div>

              <template v-else>
                <svg v-if="f.passwordRequired" class="ds-me-lock" viewBox="0 0 24 24" aria-hidden="true">
                  <path d="M7 11V8a5 5 0 0 1 10 0v3" fill="none" stroke="currentColor" stroke-width="2"/>
                  <path d="M6 11h12v10H6z" fill="none" stroke="currentColor" stroke-width="2"/>
                </svg>

                <div class="ds-me-actions ds-me-actions-desktop">
                  <button class="ds-me-action ds-me-action-danger" type="button" @click="onDelete(f)">
                    <span class="ds-me-action-ic" aria-hidden="true">🗑</span>
                    Supprimer
                  </button>
                  <button class="ds-me-action ds-me-action-primary" type="button" @click="onAccess(f)">
                    Accéder <span aria-hidden="true">→</span>
                  </button>
                </div>

                <div class="ds-me-actions ds-me-actions-mobile">
                  <svg v-if="f.passwordRequired" class="ds-me-lock" viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M7 11V8a5 5 0 0 1 10 0v3" fill="none" stroke="currentColor" stroke-width="2"/>
                    <path d="M6 11h12v10H6z" fill="none" stroke="currentColor" stroke-width="2"/>
                  </svg>

                  <div class="ds-me-morewrap">
                    <button class="ds-me-more" type="button" @click.stop="toggleMenu(f.id)" aria-label="Actions">
                      <span aria-hidden="true">⋮</span>
                    </button>

                    <div v-if="openMenuId === f.id" class="ds-me-popover" @click.stop>
                      <button type="button" class="ds-me-popitem" @click="onAccess(f)">Accéder</button>
                      <button type="button" class="ds-me-popitem is-danger" @click="onDelete(f)">Supprimer</button>
                    </div>
                  </div>
                </div>
              </template>
            </div>
          </div>

          <div v-if="!filteredFiles.length && !error" class="ds-me-empty">
            Aucun fichier.
          </div>
        </div>

        <div v-else class="ds-me-loading">Chargement...</div>
      </main>
    </div>

    <div v-if="drawerOpen" class="ds-me-drawer-overlay" @click="drawerOpen = false">
      <aside class="ds-me-drawer" @click.stop>
        <button class="ds-me-drawer-close" type="button" @click="drawerOpen = false" aria-label="Fermer">
          ✕
        </button>

        <div class="ds-me-drawer-brand">DataShare</div>

        <button class="ds-me-drawer-item is-active" type="button" @click="drawerOpen = false">
          Mes fichiers
        </button>

        <div class="ds-me-drawer-actions">
          <button class="ds-me-drawer-item" type="button" @click="goUploadFromMenu">
            Téléverser
          </button>
          <button class="ds-me-drawer-item is-logout" type="button" @click="onLogout">
            Déconnexion
          </button>
        </div>

        <div class="ds-me-drawer-footer">Copyright DataShare© 2025</div>
      </aside>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import api from "../services/api";
import { getUsername, logout } from "../api/auth";

type MeFile = {
  id: string;
  originalFileName: string;
  sizeBytes: number;
  contentType: string;
  createdAt: string;
  expiresAt: string;
  token: string;
  passwordRequired: boolean;
  tags?: string[];
};

const router = useRouter();

const loading = ref(false);
const error = ref<string | null>(null);

const files = ref<MeFile[]>([]);
const tab = ref<"all" | "active" | "expired">("all");

const drawerOpen = ref(false);
const openMenuId = ref<string | null>(null);

const username = computed(() => getUsername() ?? "Utilisateur");
const userInitial = computed(() => (username.value.trim()[0] ?? "U").toUpperCase());

function isExpired(f: MeFile): boolean {
  return new Date(f.expiresAt).getTime() <= Date.now();
}

function expiryLabel(f: MeFile): string {
  const ms = new Date(f.expiresAt).getTime() - Date.now();
  const days = Math.ceil(ms / (1000 * 60 * 60 * 24));

  if (days <= 0) return "Expiré";
  if (days === 1) return "Expire demain";
  return `Expire dans ${days} jours`;
}

function fileKind(f: MeFile): "image" | "audio" | "video" | "other" {
  const ct = (f.contentType || "").toLowerCase();
  if (ct.startsWith("image/")) return "image";
  if (ct.startsWith("audio/")) return "audio";
  if (ct.startsWith("video/")) return "video";
  return "other";
}

const filteredFiles = computed(() => {
  const all = [...files.value].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  );

  if (tab.value === "all") return all;
  if (tab.value === "active") return all.filter((f) => !isExpired(f));
  return all.filter((f) => isExpired(f));
});

async function loadFiles() {
  loading.value = true;
  error.value = null;

  try {
    const res = await api.get("/files/me");
    files.value = (res.data as MeFile[]) ?? [];
  } catch (e: any) {
    const status = e?.response?.status;
    if (status === 401) {
      await router.push("/login");
      return;
    }
    error.value = e?.response?.data?.message || e?.message || "Impossible de charger tes fichiers.";
  } finally {
    loading.value = false;
  }
}

async function onDelete(f: MeFile) {
  try {
    await api.delete(`/files/${f.id}`);
    files.value = files.value.filter((x) => x.id !== f.id);
    openMenuId.value = null;
  } catch (e: any) {
    error.value = e?.response?.data?.message || e?.message || "Suppression impossible.";
  }
}

function onAccess(f: MeFile) {
  openMenuId.value = null;
  router.push(`/download/${f.token}`);
}

function toggleMenu(id: string) {
  openMenuId.value = openMenuId.value === id ? null : id;
}

function onDocClick() {
  openMenuId.value = null;
}

function goUpload() {
  router.push("/");
}

function goUploadFromMenu() {
  drawerOpen.value = false;
  router.push("/");
}

function onLogout() {
  logout();
  router.push("/login");
}

onMounted(() => {
  loadFiles();
  document.addEventListener("click", onDocClick);
});

onBeforeUnmount(() => {
  document.removeEventListener("click", onDocClick);
});
</script>
