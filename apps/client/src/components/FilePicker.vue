<script setup>
import { ref } from 'vue'

const props = defineProps({
  id: { type: String, default: 'file-input' },
  label: { type: String, default: 'Plik' },
  accept: { type: String, default: '' },
})
const emit = defineEmits(['change'])

const inputRef = ref(null)
const fileName = ref('Brak pliku')

function onChange(e) {
  const file = e?.target?.files?.[0] || null
  fileName.value = file?.name || 'Brak pliku'
  emit('change', { file, name: fileName.value, input: inputRef.value })
}

function clear() {
  if (inputRef.value) inputRef.value.value = ''
  fileName.value = 'Brak pliku'
  emit('change', { file: null, name: fileName.value, input: inputRef.value })
}

defineExpose({ clear })
</script>

<template>
  <div class="field">
    <label class="label" :for="id">{{ label }}</label>
    <div class="filepicker">
      <input :id="id" class="sr-only" type="file" :accept="accept" ref="inputRef" @change="onChange" />
      <button type="button" class="btn secondary" @click="inputRef && inputRef.click()">WYBIERZ PLIK</button>
      <div class="filename" :class="{ placeholder: fileName === 'Brak pliku' }">{{ fileName }}</div>
    </div>
  </div>
</template>

<style scoped>
</style>
