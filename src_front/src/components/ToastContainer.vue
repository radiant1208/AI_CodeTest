<script setup lang="ts">
import { useToastStore } from '@/stores/toastStore'

const toastStore = useToastStore()
</script>

<template>
  <div class="toast-container">
    <div
      v-for="message in toastStore.messages"
      :key="message.id"
      class="toast"
      :class="`toast--${message.kind}`"
      @click="toastStore.dismiss(message.id)"
    >
      {{ message.text }}
    </div>
  </div>
</template>

<style scoped>
.toast-container {
  position: fixed;
  bottom: 16px;
  right: 16px;
  /* Vuetify의 v-app-bar/v-navigation-drawer는 훨씬 낮은 z-index를 쓰고, 자체 VSnackbar가
     10000을 쓰는 것과 동일한 기준으로 맞춰 항상 최상위에 보이도록 한다. */
  z-index: 10000;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.toast {
  min-width: 240px;
  max-width: 360px;
  padding: 10px 14px;
  border-radius: 6px;
  color: #fff;
  font-size: 14px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
  cursor: pointer;
}

.toast--info {
  background: #3b6fd6;
}

.toast--success {
  background: #2e9e5b;
}

.toast--error {
  background: #d64545;
}
</style>
