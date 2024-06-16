<script setup lang="ts">
import {onMounted, onUnmounted, ref} from 'vue';
import {addToIndex, getCounters, getQueue} from './api';
import {message} from 'ant-design-vue';
import {getItemDescription} from "@/utils";

const count = 100;

onMounted(() => {
  reloadCounters();
  loadList();
  interval.value = setInterval(() => {
    if (props.active) {
      reloadCounters();
    }
  }, 3000)
});

onUnmounted(() => {
  clearInterval(interval.value);
});

const props = defineProps({active: {type: Boolean, required: true}});
const interval = ref(0);
const list = ref([]);
const counts = ref({});
const offset = ref(0);

const statusNameAndStyle = (status: string) => {
  switch (status) {
    case 'Queued':
      return ['В очереди', '#1677ff'];
    case 'VideoIndexed':
      return ['Индекс по видео', '#52c41a'];
    case 'FullIndexed':
      return ['Проиндексировано', '#52c41a'];
    case 'Error':
      return ['Ошибки', '#ff4d4f'];
  }

  return status;
};

const reloadCounters = async () => {
  counts.value = await getCounters();
};

const loadList = async () => {
  list.value = await getQueue(count, offset.value);
}

const urlToAdd = ref('');
const addUrlToIndex = async () => {
  if (!urlToAdd.value) {
    message.warning('Введите ссылку на видео');
  } else {
      const added = await addToIndex(urlToAdd.value);
      if (added) {
        message.success('Поставлено в очередь');
        urlToAdd.value = '';
        await loadList();
      } else {
        message.error('Что-то пошло не так');
      }
  }
};

const getItemDesc = (item: any) => {
  return getItemDescription(item);
};

const changeOffset = (direction: number) => {
  let newOffset = offset.value + direction * count;
  if (newOffset < 0) newOffset = 0;
  if (newOffset !== offset.value) {
    offset.value = newOffset;
    loadList();
  }
}
</script>

<template>
  <a-card title="Состояние">
    <a-row>
      <a-col v-for="entry in Object.entries(counts)" :key="entry[0]">
        <a-statistic
            :title="statusNameAndStyle(entry[0])[0]"
            :value="entry[1]"
            style="margin-right: 50px"
            :value-style="{ color: statusNameAndStyle(entry[0])[1] }"
        />
      </a-col>
    </a-row>
  </a-card>
  <a-card title="Добавить в индекс" style="margin-top: 10px">
    <a-input-group compact>
      <a-input @keyup.enter="addUrlToIndex" v-model:value="urlToAdd" style="width: calc(100% - 100px)" placeholder="https://path.to/video.mp4"/>
      <a-button type="primary" @click="addUrlToIndex" :disabled="!urlToAdd">Добавить</a-button>
    </a-input-group>
  </a-card>
  <a-page-header title="Видеозаписи" style="margin-top: 50px;">
    <template #extra>
      <div style="margin-right: 20px;">{{offset + ' — ' + (offset - (-count))}}</div>
      <a-button key="5" @click="loadList">↻</a-button>
      <a-button key="3" @click="changeOffset(-10)">‹‹</a-button>
      <a-button key="2" @click="changeOffset(-1)">‹</a-button>
      <a-button key="1" @click="changeOffset(1)">›</a-button>
      <a-button key="4" @click="changeOffset(10)">››</a-button>
    </template>
  </a-page-header>
<a-list :data-source="list" item-layout="vertical">
  <template #renderItem="{ item }">
    <a-list-item>
      <a-list-item-meta :description="item.url">
        <template #title>
          {{ new Date(item.createdAt).toLocaleString() }}
        </template>
      </a-list-item-meta>
      <template #extra>
        <a-tag color="success" v-if="item.status === 99">проиндексировано</a-tag>
        <a-tag color="processing" v-if="item.status === 1">обработка</a-tag>
        <a-tag color="error" v-if="item.status === -1">ошибка</a-tag>
        <a-tag color="warning" v-if="item.status > 1 && item.status < 99">частично</a-tag>
        <a-tag color="default" v-if="item.status === 0">в очереди</a-tag>
      </template>
      <div v-html="getItemDesc(item)"/>
    </a-list-item>
  </template>
</a-list>
</template>

<style scoped>

</style>