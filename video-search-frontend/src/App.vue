<script setup lang="ts">
import { ref } from 'vue';
import Indexing from "@/Indexing.vue";
import Search from "@/Search.vue";
const activeKey = ref('1');
</script>

<template>
  <a-tabs v-model:activeKey="activeKey" centered>
    <a-tab-pane key="1" tab="Поиск">
      <Search/>
    </a-tab-pane>
    <a-tab-pane key="2" tab="Индексация" force-render>
      <Indexing :active="activeKey === '2'" />
    </a-tab-pane>
  </a-tabs>
</template>

<style scoped>

</style>
