<script setup>
import { ref } from 'vue'
import PasswordField from './PasswordField.vue'
import FilePicker from './FilePicker.vue'

const emit = defineEmits(['log'])

const file = ref(null)
const fileInput = ref(null)
const fileName = ref('Brak pliku')
const password = ref('')
const busy = ref(false)

function onPick(payload) {
  file.value = payload.file
  fileName.value = payload.name
  fileInput.value = payload.input
}

function parseFilenameFromCD(disposition, fallback) {
  if (!disposition) return fallback
  const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(disposition)
  const name = decodeURIComponent(match?.[1] || match?.[2] || '')
  return name || fallback
}
function triggerDownload(blob, filename) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

async function submit() {
  if (!file.value) { emit('log', { level: 'error', text: 'Wybierz plik .enc do odszyfrowania.' }); return }
  if (!password.value) { emit('log', { level: 'error', text: 'Podaj hasło.' }); return }
  busy.value = true
  emit('log', { level: 'info', text: `Deszyfrowanie: ${fileName.value}` })
  try {
    const fd = new FormData()
    fd.append('File', file.value)
    fd.append('Password', password.value)
    const res = await fetch('/api/File/decrypt', { method: 'POST', body: fd })
    if (!res.ok) {
      const text = await res.text()
      throw new Error(text || 'Błąd podczas deszyfrowania.')
    }
    const blob = await res.blob()
    const cd = res.headers.get('content-disposition')
    const name = parseFilenameFromCD(cd, 'plik')
    triggerDownload(blob, name)
    emit('log', { level: 'ok', text: 'Plik odszyfrowany pomyślnie.' })
  } catch (e) {
    emit('log', { level: 'error', text: e.message || 'Wystąpił błąd.' })
  } finally {
    busy.value = false
  }
}

function clearFile() {
  if (fileInput.value) fileInput.value.value = ''
  file.value = null
  fileName.value = 'Brak pliku'
}
</script>

<template>
  <form @submit.prevent="submit" class="grid" novalidate>
    <FilePicker id="dec-file" label="Plik .enc" accept=".enc" @change="onPick" />
    <PasswordField id="dec-password" v-model="password" label="Hasło" autocomplete="current-password" />

    <div class="divider"></div>
    <div class="footer">
      <div class="row">
        <button type="button" class="btn secondary" :disabled="busy" @click="clearFile">Wyczyść</button>
        <button type="submit" class="btn" :disabled="busy">
          <span v-if="busy" class="spinner" />
          <span>{{ busy ? 'Odszyfrowywanie...' : 'Odszyfruj plik' }}</span>
        </button>
      </div>
    </div>
  </form>
</template>

<style scoped>
</style>

