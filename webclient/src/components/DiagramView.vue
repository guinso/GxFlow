<script setup>
  import { ref } from 'vue'
  import { VueFlow } from '@vue-flow/core'
  import { Background } from '@vue-flow/background'

  // these are our nodes
  const nodes = ref([
    // an input node, specified by using `type: 'input'`
    {
      id: '1',
      type: 'input',
      position: { x: 250, y: 5 },
      // all nodes can have a data object containing any data you want to pass to the node
      // a label can property can be used for default nodes
      data: { label: 'Node 1' },
    },

    // default node, you can omit `type: 'default'` as it's the fallback type
    {
      id: '2',
      position: { x: 100, y: 100 },
      data: { label: 'Node 2' },
    },

    // An output node, specified by using `type: 'output'`
    {
      id: '3',
      type: 'output',
      position: { x: 400, y: 200 },
      data: { label: 'Node 3' },
    },
  ])

  // these are our edges
  const edges = ref([
    // default bezier edge
    // consists of an edge id, source node id and target node id
    {
      id: 'e1->2',
      source: '1',
      target: '2',
    },


    // a custom edge, specified by using a custom type name
    // we choose `type: 'special'` for this example
    {
      id: 'e1->3',
      source: '1',
      target: '3',
    },
  ])
</script>

<template>
  <VueFlow :nodes="nodes" :edges="edges">
    <Background/>
  </VueFlow>
</template>

<style>
  /* import the necessary styles for Vue Flow to work */
  @import '@vue-flow/core/dist/style.css';

  /* import the default theme, this is optional but generally recommended */
  @import '@vue-flow/core/dist/theme-default.css';
</style>
