<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'

const props = defineProps({
  modelValue: { type: String, default: '' },
  options: { type: Array, default: () => [] }, // [{ label, value }]
  id: { type: String, default: undefined },
  name: { type: String, default: undefined },
  placeholder: { type: String, default: 'Wybierz…' },
})
const emit = defineEmits(['update:modelValue', 'change'])

const open = ref(false)
const root = ref(null)
const highlighted = ref(-1)

const selectedIndex = computed(() => props.options.findIndex(o => o.value === props.modelValue))
const selectedLabel = computed(() => props.options[selectedIndex.value]?.label ?? props.placeholder)

function toggle() { open.value = !open.value; if (open.value) highlighted.value = selectedIndex.value }
function close() { open.value = false }
function selectAt(index) {
  if (index < 0 || index >= props.options.length) return
  const opt = props.options[index]
  emit('update:modelValue', String(opt.value))
  emit('change', opt.value)
  highlighted.value = index
  close()
}

function onKeyDown(e) {
  if (!open.value && (e.key === 'ArrowDown' || e.key === 'ArrowUp' || e.key === 'Enter' || e.key === ' ')) {
    e.preventDefault(); open.value = true; highlighted.value = selectedIndex.value >= 0 ? selectedIndex.value : 0; return
  }
  if (!open.value) return
  switch (e.key) {
    case 'Escape': e.preventDefault(); close(); break
    case 'ArrowDown': e.preventDefault(); highlighted.value = Math.min((highlighted.value < 0 ? -1 : highlighted.value) + 1, props.options.length - 1); break
    case 'ArrowUp': e.preventDefault(); highlighted.value = Math.max((highlighted.value < 0 ? props.options.length : highlighted.value) - 1, 0); break
    case 'Home': e.preventDefault(); highlighted.value = 0; break
    case 'End': e.preventDefault(); highlighted.value = props.options.length - 1; break
    case 'Enter':
    case ' ': e.preventDefault(); if (highlighted.value >= 0) selectAt(highlighted.value); break
  }
}

function onClickOutside(ev) {
  if (!root.value) return
  if (!root.value.contains(ev.target)) close()
}

onMounted(() => document.addEventListener('mousedown', onClickOutside))
onBeforeUnmount(() => document.removeEventListener('mousedown', onClickOutside))
</script>

<template>
  <div class="fselect" :id="id" ref="root">
    <input v-if="name" type="hidden" :name="name" :value="modelValue ?? ''" />
    <button type="button" class="fselect__button" :aria-expanded="open ? 'true' : 'false'" @click="toggle" @keydown="onKeyDown">
      <span class="fselect__label">{{ selectedLabel }}</span>
      <svg class="fselect__icon" width="16" height="16" viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" d="M7 10l5 5 5-5z"/></svg>
    </button>
    <ul v-show="open" class="fselect__list" role="listbox" :aria-activedescendant="highlighted >= 0 ? 'opt-' + highlighted : undefined">
      <li v-for="(opt, i) in options" :key="opt.value" class="fselect__option" :id="'opt-' + i" role="option" :aria-selected="i===selectedIndex"
          :class="{ active: i===highlighted, selected: i===selectedIndex }" @click="selectAt(i)" @mousemove="() => highlighted = i">
        {{ opt.label }}
      </li>
    </ul>
  </div>
  
</template>

<style scoped>
.fselect { position: relative; width: 100%; }
.fselect__button {
  width: 100%;
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(255,255,255,0.08);
  color: var(--text);
  border-radius: 10px;
  padding: 12px 14px;
  display: flex; align-items: center; justify-content: space-between; gap: 10px;
  cursor: pointer; outline: none;
}
.fselect__button:focus { border-color: rgba(34,211,238,.45); box-shadow: 0 0 0 4px rgba(14,165,233,.15); }
.fselect__icon { color: var(--muted); }

.fselect__list {
  position: absolute; left: 0; right: 0; top: calc(100% + 6px); z-index: 20;
  background: linear-gradient(180deg, var(--panel) 0%, var(--panel-2) 100%);
  border: 1px solid rgba(255,255,255,0.08);
  border-radius: 10px;
  padding: 6px;
  max-height: 220px; overflow: auto;
  box-shadow: 0 14px 30px rgba(0,0,0,0.35);
}
.fselect__option {
  list-style: none;
  padding: 10px 12px; border-radius: 8px;
  color: var(--text);
  cursor: pointer;
}
.fselect__option:hover, .fselect__option.active { background: rgba(14,165,233,.12); }
.fselect__option.selected { outline: 1px dashed rgba(34,211,238,.35); }
</style>

