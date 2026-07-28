<script setup lang="ts">
import DashboardPage from '@/pages/DashboardPage.vue'
</script>

<template>
  <DashboardPage />
</template>

<style>
/* 뷰포트를 벗어나는 스크롤이 절대 생기지 않도록 루트 체인 전체 높이를 고정한다. */
html,
body {
  margin: 0;
  padding: 0;
  height: 100%;
  overflow: hidden;
}

#app,
.v-application {
  height: 100%;
}

/* min-height만으로는 flex 자식(.v-main 등)이 percentage 높이 체인을 해석할 "definite height"가
   되지 못해 캔버스 하단이 잘리는 원인이 되므로, 명시적인 뷰포트 높이로 고정한다. */
.v-application__wrap {
  height: 100vh;
  height: 100dvh;
}

* {
  box-sizing: border-box;
}

/* 요구사항: 폰트 크기 최대 12px. 아이콘 글리프는 텍스트가 아니므로 제외한다. */
.v-application {
  font-size: 12px;
}

.v-application :not(.v-icon) {
  font-size: 12px !important;
}

.v-application .v-icon {
  font-size: 16px !important;
}
</style>
