<script setup lang="ts">
  import NodeComponent from './NodeComponent.vue'
  
  interface Props {
    id: string
    data: {
      label: string
      script: string
    }
  }

  const props = defineProps<Props>()
  
</script>

<template>
  <NodeComponent>
    <template #icon>
      <span>&lt;/&gt;</span>
    </template>
    <p>{{ props.data.script }}</p>
  </NodeComponent>
</template>

<style scoped>

</style>
