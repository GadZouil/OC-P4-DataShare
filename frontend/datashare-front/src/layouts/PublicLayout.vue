<template>
  <div class="ds-page">
    <header class="ds-header">
      <div class="ds-brand">DataShare</div>

      <div class="ds-header-right">
        <slot name="header-actions">
          <RouterLink class="ds-header-action" :to="actionTo">{{ actionLabel }}</RouterLink>
        </slot>
      </div>
    </header>

    <main class="ds-content">
      <slot />
    </main>

    <footer class="ds-footer">Copyright DataShare© 2025</footer>
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import { getJwt } from "../api/auth";

  const route = useRoute();
  const isLoggedIn = computed(() => {
    route.fullPath; // dépendance reactive
    return !!getJwt();
  });

  const props = defineProps<{
    headerActionLabel: string;
    headerActionTo: string;
  }>();

  const actionLabel = computed(() => (isLoggedIn.value ? "Mon espace" : (props.headerActionLabel ?? "Se connecter")));
  const actionTo = computed(() => (isLoggedIn.value ? "/me" : (props.headerActionTo ?? "/login")));
</script>