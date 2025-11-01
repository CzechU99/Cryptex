<script setup>
import { ref, computed } from "vue"

const props = defineProps({
  modelValue: { type: String, default: '' },
  id: { type: String, default: 'password' },
  label: { type: String, default: 'Hasło' },
  placeholder: { type: String, default: 'Co najmniej 8 znaków' },
  autocomplete: { type: String, default: 'new-password' },
  showStrength: { type: Boolean, default: false },
  showRandom: { type: Boolean, default: true },
  includeUsernameHidden: { type: Boolean, default: true },
})
const emit = defineEmits(['update:modelValue'])

const show = ref(false)

const strength = computed(() => {
  const p = props.modelValue || ''
  let s = 0
  if (p.length >= 8) s++
  if (/[A-Z]/.test(p) && /[a-z]/.test(p)) s++
  if (/\d/.test(p)) s++
  if (/[^A-Za-z0-9]/.test(p)) s++
  return s
})

function genPassword() {
  const length = 16
  const lowers = 'abcdefghjkmnpqrstuvwxyz'
  const uppers = 'ABCDEFGHJKMNPQRSTUVWXYZ'
  const digits = '23456789'
  const symbols = '!@#$%^&*()-_=+[]{};:,.?'
  const pick = (str) => str[Math.floor(Math.random() * str.length)]
  const all = lowers + uppers + digits + symbols
  const arr = [pick(lowers), pick(uppers), pick(digits), pick(symbols)]
  for (let i = arr.length; i < length; i++) arr.push(pick(all))
  for (let i = arr.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    ;[arr[i], arr[j]] = [arr[j], arr[i]]
  }
  emit('update:modelValue', arr.join(''))
  show.value = true
}
</script>

<template>
  <div class="field">
    <label class="label" :for="id">{{ label }}</label>
    <template v-if="includeUsernameHidden">
      <label class="sr-only" :for="id + '-username'">Nazwa użytkownika (opcjonalnie)</label>
      <input :id="id + '-username'" name="username" class="sr-only" type="text" autocomplete="username" autocapitalize="off" spellcheck="false" />
    </template>
    <div class="row">
      <input :id="id" name="password" :type="show ? 'text' : 'password'" class="input grow" :placeholder="placeholder" :autocomplete="autocomplete" :value="modelValue" @input="e => emit('update:modelValue', e.target.value)" />
      <button type="button" class="btn secondary" @click="show=!show" :aria-label="show ? 'Ukryj hasło' : 'Pokaż hasło'"></button>
      <button v-if="showRandom" type="button" class="btn secondary" @click="genPassword" title="Wygeneruj mocne hasło">Losowe</button>
    </div>
    <template v-if="showStrength">
      <div class="muted">Siła hasła</div>
      <div class="strength" :class="'level-' + strength">
        <div v-for="i in 4" :key="i" class="seg" :class="{ filled: strength >= i }"></div>
      </div>
    </template>
  </div>
</template>

<style scoped>
</style>

