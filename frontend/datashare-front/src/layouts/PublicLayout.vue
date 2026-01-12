<template>
  <div class="ds-page">
    <header class="ds-header">
      <div class="ds-brand">DataShare</div>

      <RouterLink class="ds-header-action" :to="actionTo">
        {{ actionLabel }}
      </RouterLink>
    </header>

    <main class="ds-content">
      <slot />
    </main>

    <footer class="ds-footer">Copyright DataShare© 2025</footer>
  </div>
</template>

<script setup lang="ts">
  import { computed } from "vue";
  import { isAuthenticated } from "../api/auth";

  const props = defineProps<{
    headerActionLabel: string;
    headerActionTo: string;
  }>();

  const actionLabel = computed(() =>
    isAuthenticated.value ? "Mon espace" : (props.headerActionLabel ?? "Se connecter")
  );

  const actionTo = computed(() =>
    isAuthenticated.value ? "/" : (props.headerActionTo ?? "/login")
  );
</script>
