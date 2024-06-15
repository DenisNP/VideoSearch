<script setup lang="ts">
import {ref} from "vue";
import { hints, search } from "./api";

const query = ref('');
const onSearch = () => {

};

const hintsToShow = ref([]);
const showHints = async () => {
  if (!query.value) {
    return;
  }

  const words = await hints(query.value);
  if (words.length === 0) return;

  const toShowRaw = [
      words[0],
    words[Math.max(Math.floor(words.length / 2) - 1, 0)],
    words[words.length - 1]
  ];
  const toShow = [...new Set(toShowRaw)];
  hintsToShow.value = toShow;
};
</script>

<template>
<a-card title="Поиск">
  <a-tag v-for="h in hintsToShow" :key="h">
    {{h}}
  </a-tag>
  <a-input-search
      v-model:value="query"
      @change="showHints"
      placeholder="введите поисковый запрос"
      enter-button
      @search="onSearch"
  />
</a-card>
</template>

<style scoped>

</style>