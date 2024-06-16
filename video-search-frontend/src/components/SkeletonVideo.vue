<script setup lang="ts">
import {nextTick, onMounted, onUnmounted, ref} from "vue";

const props = defineProps({url: String});
const loaded = ref(false);

onMounted(() => {
  window.addEventListener('scroll', handleScroll);
  nextTick(() => {
    checkLoad();
  });
});
onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll);
});

const handleScroll = () => {
  checkLoad();
};

const checkLoad = () => {
  if (vid.value) {
    const rect = vid.value.getBoundingClientRect();
    if (rect.top < window.document.documentElement.clientHeight) {
      loaded.value = true;
      window.removeEventListener('scroll', handleScroll);
    }
  }
};

const vid = ref(null);
</script>

<template>
  <div ref="vid"/>
  <video
      controls
      :src="props.url"
      width="200px"
      style="min-height: 355.55px;"
      v-if="loaded"
  />
</template>

<style scoped>

</style>