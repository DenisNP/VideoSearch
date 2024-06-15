<script setup lang="ts">
import {ref} from "vue";
import { hints, search } from "./api";
import SkeletonVideo from "@/components/SkeletonVideo.vue";
import {getItemDescription} from "@/utils";

const query = ref('');
const results = ref([]);
const onSearch = async () => {
  results.value = await search(query.value);
};

const hintsToShow = ref([] as string[]);
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
  const toShow: string[] = [...new Set(toShowRaw)];
  hintsToShow.value = toShow;
};

const getHint = (h: string, idx: number) => {
  return h + (idx === 0 ? ' <span style="color: #b3b3b3">(tab)</span>' : '' );
};

const insertHint = () => {
  if (!hintsToShow.value || !hintsToShow.value.length) return;
  const first = hintsToShow.value[0] + ' ';
  const inputArr = query.value.trim().split(' ');
  if (inputArr.length === 0) {
    query.value = first;
  } else {
    inputArr[inputArr.length - 1] = first;
    query.value = inputArr.join(' ');
  }
};

const getItemDesc = (item: any) => {
  return getItemDescription(item);
};
</script>

<template>
<a-card title="Поиск">
  <a-tag v-for="(h, idx) in hintsToShow" :key="h">
    <span v-html="getHint(h, idx)"></span>
  </a-tag>
  <a-input-search
      style="margin-top: 10px"
      v-model:value="query"
      @change="showHints"
      @keydown.tab.prevent="insertHint"
      placeholder="введите поисковый запрос"
      enter-button
      @search="onSearch"
  />
</a-card>
  <a-list :data-source="results" item-layout="vertical" style="margin-top: 50px">
    <template #renderItem="{ item }">
      <a-list-item>
        <a-list-item-meta :description="item.video.url"/>
        <template #extra>
          <skeleton-video :url="item.video.url"/>
        </template>
        <div>{{ item.avgDist }}</div>
        <div v-html="getItemDesc(item.video)"/>
      </a-list-item>
    </template>
  </a-list>
</template>

<style scoped>

</style>