<script setup lang="ts">import NodeComponent from './NodeComponent.vue'

  interface Props {
    id: string
    data: {
      label: string
      script: string
    }
  }

  const props = defineProps<Props>()</script>

<template>
  <NodeComponent>
    <template #icon>
      <svg class="fork-node">
        <line x1="30" y1="30" x2="40" y2="30" style="stroke:black;stroke-width:2" />

        <line x1="20" y1="25" x2="30" y2="25" style="stroke:black;stroke-width:2" />
        <line x1="20" y1="35" x2="30" y2="35" style="stroke:black;stroke-width:2" />

        <line x1="30" y1="20" x2="30" y2="40" style="stroke:black;stroke-width:2" />
      </svg>
    </template>
  </NodeComponent>
</template>

<style scoped>
  .fork-node {
    width: 50px;
    height: 50px;
    margin: auto;
  }
</style>
