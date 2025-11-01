<script setup>
import { ref } from 'vue'
import PasswordField from './PasswordField.vue'
import FilePicker from './FilePicker.vue'

const emit = defineEmits(['log'])

const file = ref(null)
const fileName = ref('Brak pliku')
const password = ref('')
const busy = ref(false)
const pickerRef = ref(null)

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
  if (!file.value) {
    return emit('log', { level: 'error', text: 'Wybierz plik .enc do odszyfrowania.' })
  }
  if (!password.value) {
    return emit('log', { level: 'error', text: 'Podaj hasło.' })
  }
  emit('log', { level: 'info', text: `Deszyfrowanie: ${fileName.value}` })

  busy.value = true

  try {
    const form = new FormData()
    form.append('File', file.value)
    form.append('Password', password.value)

    const res = await fetch('/api/decrypt', { method: 'POST', body: form })

    if (!res.ok) { 
      throw new Error(await res.text() || 'Błąd podczas deszyfrowania.') 
    }

    const blob = await res.blob()
    const name = getFileName(res.headers.get('content-disposition'), fileName.value.replace(/\.enc$/i, '') || 'plik')
    
    download(blob, name)

    emit('log', { level: 'ok', text: 'Plik odszyfrowany pomyślnie.' })
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

  if (pickerRef.value?.clear) {
    pickerRef.value.clear()
  } else {
    const el = pickerRef.value?.$el || pickerRef.value
    const input = el?.querySelector?.('input[type="file"]')
    if (input) input.value = ''
  }

  emit('log', { level: 'info', text: 'Formularz odszyfrowywania wyczyszczony.' })
}
</script>


<template>
  <form @submit.prevent="submit" class="grid rowgap-lg" novalidate>
    <FilePicker ref="pickerRef" id="dec-file" label="Plik .enc" accept=".enc" @change="onPick" />
    <PasswordField id="dec-password" v-model="password" :show-random="false" label="Hasło" autocomplete="current-password" />

    <div class="footer">
      <div class="row">
        <button type="submit" class="btn" :disabled="busy">
          <span v-if="busy" class="spinner" />
          <span>{{ busy ? 'Odszyfrowywanie...' : 'Odszyfruj plik' }}</span>
        </button>
        <button type="button" class="btn secondary" :disabled="busy" @click="clearForm">Wyczyść</button>
      </div>
    </div>
  </form>
</template>

<style scoped>
</style>

