<script setup>
import { ref, watch, onMounted, nextTick } from 'vue'

const props = defineProps({
  logs: { type: Array, default: () => [] },
})

const bodyRef = ref(null)

function scrollToBottom() {
  const el = bodyRef.value
  if (!el) return
  el.scrollTop = el.scrollHeight
}

onMounted(() => {
  scrollToBottom()
})

watch(() => props.logs.length, async () => {
  await nextTick()
  scrollToBottom()
})
</script>

<template>
  <div class="console card" role="log" aria-live="polite">
    <div class="console-title">Konsola</div>
    <div class="console-body" ref="bodyRef">
      <div v-if="!logs.length" class="console-placeholder"><span class="time">[--:--:--]</span> Brak wpisów</div>
      <template v-else>
        <div v-for="(l, i) in logs" :key="i" class="console-line" :class="l.level">
          <span class="time">[{{ l.time }}]</span>
          <span class="text">{{ l.text }}</span>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
</style>
