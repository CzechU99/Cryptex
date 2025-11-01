<script setup>
import { ref } from 'vue'
import FancySelect from './FancySelect.vue'
import PasswordField from './PasswordField.vue'
import FilePicker from './FilePicker.vue'

const emit = defineEmits(['log'])

const file = ref(null)
const pickerRef = ref(null)
const fileInput = ref(null)
const fileName = ref('Brak pliku')
const password = ref('')
const algorithm = ref('AES-GCM')
const algOptions = [
  { label: 'AES-GCM', value: 'AES-GCM' },
  { label: 'ChaCha20-Poly1305', value: 'ChaCha20-Poly1305' },
]
const expire = ref('')
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
  if (!file.value) { emit('log', { level: 'error', text: 'Wybierz plik do zaszyfrowania.' }); return }
  if (!password.value) { emit('log', { level: 'error', text: 'Podaj hasło.' }); return }
  busy.value = true
  emit('log', { level: 'info', text: `Szyfrowanie: ${fileName.value} (${algorithm.value})` })
  try {
    const fd = new FormData()
    fd.append('File', file.value)
    fd.append('Password', password.value)
    fd.append('Algorithm', algorithm.value)
    if (expire.value) {
      const date = new Date(expire.value)
      fd.append('ExpireTime', date.toISOString())
    }
    const res = await fetch('/api/File/encrypt', { method: 'POST', body: fd })
    if (!res.ok) {
      const text = await res.text()
      throw new Error(text || 'Błąd podczas szyfrowania.')
    }
    const blob = await res.blob()
    const fallback = `${fileName.value}.enc`
    const cd = res.headers.get('content-disposition')
    const name = parseFilenameFromCD(cd, fallback)
    triggerDownload(blob, name)
    emit('log', { level: 'ok', text: 'Plik zaszyfrowany pomyślnie.' })
  } catch (e) {
    emit('log', { level: 'error', text: e.message || 'Wystąpił błąd.' })
  } finally {
    busy.value = false
  }
}

function clearAll() {
  if (pickerRef.value && typeof pickerRef.value.clear === 'function') {
    try { pickerRef.value.clear() } catch {}
  }
  if (fileInput.value) fileInput.value.value = ''
  file.value = null
  fileName.value = 'Brak pliku'
  password.value = ''
  expire.value = ''
  emit('log', { level: 'info', text: 'Wyczyszczono formularz szyfrowania.' })
}
</script>

<template>
  <form @submit.prevent="submit" class="grid rowgap-lg" novalidate>
    <FilePicker ref="pickerRef" id="enc-file" label="Plik do zaszyfrowania" @change="onPick" />

    <PasswordField id="enc-password" v-model="password" label="Hasło" :show-strength="true" autocomplete="new-password" />

    <div class="field">
      <label class="label" for="enc-algo">Algorytm</label>
      <FancySelect id="enc-algo" name="algorithm" :options="algOptions" v-model="algorithm" />
    </div>

    <div class="field">
      <label class="label" for="enc-expire">Data wygaśnięcia (opcjonalnie)</label>
      <input id="enc-expire" name="expire" class="input" type="datetime-local" v-model="expire" autocomplete="off" />
      <div class="muted">Jeśli ustawisz, plik nie odszyfruje się po upływie daty.</div>
    </div>

    <div class="footer">
      <div class="row">
        <button type="submit" class="btn" :disabled="busy">
          <span v-if="busy" class="spinner" />
          <span>{{ busy ? 'Szyfrowanie...' : 'Zaszyfruj plik' }}</span>
        </button>
        <button type="button" class="btn secondary" :disabled="busy" @click="clearAll">Wyczyść</button>
      </div>
    </div>
  </form>
</template>

<style scoped>
</style>
