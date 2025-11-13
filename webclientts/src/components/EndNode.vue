<script setup lang="ts">
import { Handle, Position } from '@vue-flow/core'

</script>

<template>
  <div class="end-node">
    <div class="end-node2"></div>
    <Handle type="target" :position="Position.Left" />
  </div>
  
</template>

<style scoped>
  .end-node {
    width: 50px;
    height: 50px;
    border-color: black;
    border-width: 1px;
    border-style: solid;
    border-radius: 50%;
  }

  .end-node2 {
    margin-left: 9px;
    margin-top: 9px;
    width: 30px;
    height: 30px;
    background-color: black;
    border-width: 1px;
    border-radius: 50%;
  }
</style>
