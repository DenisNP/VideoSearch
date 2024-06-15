<script setup lang="ts">
import {ref} from "vue";

const props = defineProps({url: String});
const loading = ref(true);
</script>

<template>
  <video
      controls
      :src="props.url"
      width="200px"
      style="min-height: 355.55px;"
      @loadeddata="loading = false"
  />
</template>

<style scoped>

</style>