<script setup lang="ts">
defineProps<{ visible: boolean }>()
</script>

<template>
  <div v-if="visible" class="overlay">
    <div class="dialog">
      <div class="spinner" />
      <p>경로 탐색 중...</p>
    </div>
  </div>
</template>

<style scoped>
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 900;
}

.dialog {
  background: #fff;
  border-radius: 8px;
  padding: 32px 40px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  color: #222;
}

.spinner {
  width: 36px;
  height: 36px;
  border: 4px solid #dde3ef;
  border-top-color: #3b6fd6;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
