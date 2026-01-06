<template>
  <div class="ds-page">
    <header class="ds-header">
      <div class="ds-brand">DataShare</div>

      <RouterLink class="ds-header-action" :to="headerActionTo">
        {{ headerActionLabel }}
      </RouterLink>
    </header>

    <main class="ds-content">
      <slot />
    </main>

    <footer class="ds-footer">Copyright DataShare© 2025</footer>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  headerActionLabel: string;
  headerActionTo: string;
}>();
</script>
