<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'

const props = defineProps({
  modelValue: { type: String, default: '' },
  options: { type: Array, default: () => [] }, 
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

function toggle() { 
  open.value = !open.value; 
  if (open.value) 
    highlighted.value = selectedIndex.value 
}

function close() { 
  open.value = false 
}

function selectAt(index) {
  if (index < 0 || index >= props.options.length) return
  const opt = props.options[index]
  emit('update:modelValue', String(opt.value))
  emit('change', opt.value)
  highlighted.value = index
  close()
}

function onClickOutside(ev) {
  if (!root.value) 
    return
  
  if (!root.value.contains(ev.target)) 
    close()
}

onMounted(() => document.addEventListener('mousedown', onClickOutside))
onBeforeUnmount(() => document.removeEventListener('mousedown', onClickOutside))
</script>

<template>
  <div class="fselect" :id="id" ref="root">
    <input v-if="name" type="hidden" :name="name" :value="modelValue ?? ''" />
    <button type="button" class="fselect__button" :aria-expanded="open ? 'true' : 'false'" @click="toggle">
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