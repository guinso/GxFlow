<script setup lang="ts">
  import { markRaw, ref } from 'vue'
  import type { Node, Edge } from '@vue-flow/core'
  import { Position, VueFlow, useVueFlow } from '@vue-flow/core'
  import { Background } from '@vue-flow/background'

  import StartNode from './StartNode.vue'
  import EndNode from './EndNode.vue'
  import ScriptNode from './ScriptNode.vue'
  import ForkNode from './ForkNode.vue'
  import MergeNode from './MergeNode.vue'

  const nodeTypes = {
    start: markRaw(StartNode),
    end: markRaw(EndNode),
    cscript: markRaw(ScriptNode),
    fork: markRaw(ForkNode),
    merge: markRaw(MergeNode)
  }

  //enable interactive create edge between nodes
  const { onConnect, addEdges } = useVueFlow()
  onConnect((connection) => {
    addEdges(connection)
  })

  // these are our nodes
  const nodes = ref<Node[]>([
    {
      id: '1',
      type: 'start',
      position: { x: 50, y: 50 },
    },
    {
      id: '2',
      type: 'end',
      position: { x: 450, y: 50 },
    },
    {
      id: '3',
      type: 'cscript',
      position: { x: 200, y: 50 },
      data: { script: 'int k = 9;' }
    },
    {
      id: '4',
      type: 'fork',
      position: { x: 600, y: 150 }
    },
    {
      id: '5',
      type: 'merge',
      position: { x: 650, y: 200 }
    },

    {
      id: 'a',
      position: { x: 500, y: 200 },
      data: {label: 'a'}
    },
    {
      id: 'b',
      position: { x: 500, y: 400 },
      data: { label: 'b' }
    },
  ])

  // these are our edges
  const edges = ref<Edge[]>([
    {
      id: 'a->b',
      source: '1',
      target: '3',
    },
    {
      id: 'b->c',
      source: '3',
      target: '2',
    },
    //{
    //  id: 'e2->3',
    //  source: '3',
    //  target: '2',
    //},
  ])
</script>

<template>
  <VueFlow :nodes="nodes" :edges="edges" :nodeTypes="nodeTypes">
    <Background/>
  </VueFlow>
</template>

<style>
  /* import the necessary styles for Vue Flow to work */
  @import '@vue-flow/core/dist/style.css';

  /* import the default theme, this is optional but generally recommended */
  @import '@vue-flow/core/dist/theme-default.css';
</style>
