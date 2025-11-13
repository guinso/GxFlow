<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'

</script>

<template>
  <div class="start-node">
    <Handle type="source" :position="Position.Right" />
  </div>
  
</template>

<style scoped>
  .start-node {
    width: 50px;
    height: 50px;
    background-color: black;
    border-radius: 50%;
  }
</style>
