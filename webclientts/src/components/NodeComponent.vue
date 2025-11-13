<script setup lang="ts">
  import { Handle, Position } from '@vue-flow/core'
  import { ref } from 'vue'

  const showContent = ref(true);

  function showHideContent() {
    showContent.value = !showContent.value;
  }
</script>

<template>
  <div class="node">
    <Handle type="target" :position="Position.Left" />
    <button @click="showHideContent">show/hide</button>
    <slot name="icon"></slot>
    <div v-show="showContent">
      <slot></slot>
    </div>
    <Handle type="source" :position="Position.Right" />
  </div>
</template>

<style scoped>
  .node {
    background: #f8f8f8;
    text-align: center;
    border: 1px solid #ccc;
    border-radius: 5px;
    padding: 2px;
  }
</style>
