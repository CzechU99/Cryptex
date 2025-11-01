<script setup>
import { ref } from "vue"
import EncryptForm from './components/EncryptForm.vue'
import DecryptForm from './components/DecryptForm.vue'
import ConsoleLog from './components/ConsoleLog.vue'

const tab = ref("encrypt")

const logs = ref([])
function onLog(entry) {
  const now = new Date()
  logs.value.push({ level: entry.level || 'info', text: entry.text || String(entry), time: now.toLocaleTimeString() })
  if (logs.value.length > 200) logs.value.shift()
}
</script>

<template>
  <div class="container">
    <div class="card">
      <div class="brand">
        <div class="dot" />
        <h1>CRYPTEX</h1>
        <div class="sub">Szyfruj i odszyfruj pliki przy użyciu Twojego hasła.</div>
      </div>

      <div class="tabs">
        <div class="tab" :class="{ active: tab==='encrypt' }" @click="tab='encrypt'">SZYFROWANIE</div>
        <div class="tab" :class="{ active: tab==='decrypt' }" @click="tab='decrypt'">DESZYFROWANIE</div>
      </div>

      <div class="divider"></div>

      <div v-if="tab==='encrypt'" style="margin-top: 12px;">
        <EncryptForm @log="onLog" />
      </div>

      <div v-if="tab==='decrypt'" style="margin-top: 12px;">
        <DecryptForm @log="onLog" />
      </div>

      <div class="divider" />
      <ConsoleLog :logs="logs" />
    </div>

  </div>
</template>

<style scoped>
</style>
