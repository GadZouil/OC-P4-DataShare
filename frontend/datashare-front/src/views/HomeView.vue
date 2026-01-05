<script setup lang="ts">
import { ref, onMounted } from "vue";
import api from "@/services/api";

const status = ref<string>("Chargement...");

onMounted(async () => {
  try {
    const res = await api.get("/health");
    status.value = JSON.stringify(res.data);
  } catch {
    status.value = "API indisponible";
  }
});
</script>

<template>
  <main style="padding: 24px">
    <h1>DataShare</h1>
    <p>API health : {{ status }}</p>
  </main>
</template>
