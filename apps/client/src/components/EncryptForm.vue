<script setup>
import { ref } from 'vue'
import FancySelect from './FancySelect.vue'
import PasswordField from './PasswordField.vue'
import FilePicker from './FilePicker.vue'

const emit = defineEmits(['log'])

const file = ref(null)
const fileName = ref('Brak pliku')
const password = ref('')
const algorithm = ref('AES-GCM')
const expire = ref('')
const busy = ref(false)
const pickerRef = ref(null)

const algOptions = [
  { label: 'AES-GCM', value: 'AES-GCM' },
  { label: 'ChaCha20-Poly1305', value: 'ChaCha20-Poly1305' }
]

function onPick({ file: f, name }) {
  file.value = f
  fileName.value = name
}

const getFileName = (cd, fallback) => {
  const m = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(cd || '')
  return decodeURIComponent(m?.[1] || m?.[2] || '') || fallback
}

const download = (blob, name) => {
  const url = URL.createObjectURL(blob)
  Object.assign(document.createElement('a'), { href: url, download: name }).click()
  URL.revokeObjectURL(url)
}

async function submit() {
  if (!file.value) return emit('log', { level: 'error', text: 'Wybierz plik.' })
  if (!password.value) return emit('log', { level: 'error', text: 'Podaj hasło.' })

  busy.value = true
  emit('log', { level: 'info', text: `Szyfrowanie: ${fileName.value} (${algorithm.value})` })

  try {
    const form = new FormData()
    form.append('File', file.value)
    form.append('Password', password.value)
    form.append('Algorithm', algorithm.value)
    if (expire.value) form.append('ExpireTime', new Date(expire.value).toISOString())

    const res = await fetch('/api/encrypt', { method: 'POST', body: form })
    if (!res.ok) throw new Error(await res.text() || 'Błąd podczas szyfrowania.')

    const blob = await res.blob()
    const name = getFileName(res.headers.get('content-disposition'), `${fileName.value}.enc`)
    download(blob, name)
    emit('log', { level: 'ok', text: 'Plik zaszyfrowany pomyślnie.' })
  } catch (err) {
    emit('log', { level: 'error', text: err.message || 'Wystąpił błąd.' })
  } finally {
    busy.value = false
  }
}

function clearForm() {
  file.value = null
  fileName.value = 'Brak pliku'
  password.value = ''
  expire.value = ''

  if (pickerRef.value?.clear) {
    pickerRef.value.clear()
  } else {
    const el = pickerRef.value?.$el || pickerRef.value
    const input = el?.querySelector?.('input[type="file"]')
    if (input) input.value = ''
  }

  emit('log', { level: 'info', text: 'Formularz wyczyszczony.' })
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
        <button type="button" class="btn secondary" :disabled="busy" @click="clearForm">Wyczyść</button>
      </div>
    </div>
  </form>
</template>

<style scoped>
</style>
