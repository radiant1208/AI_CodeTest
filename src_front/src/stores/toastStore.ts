import { defineStore } from 'pinia'

export type ToastKind = 'info' | 'success' | 'error'

export interface ToastMessage {
  id: number
  kind: ToastKind
  text: string
}

const AUTO_DISMISS_MS = 4000

/** 맵 업로드/파라미터 수정/탐색 시작·성공·실패·취소 등 모든 이벤트를 Toast로 노출하기 위한 큐. */
export const useToastStore = defineStore('toast', {
  state: () => ({
    messages: [] as ToastMessage[],
    _nextId: 1,
  }),
  actions: {
    push(kind: ToastKind, text: string) {
      const id = this._nextId++
      this.messages.push({ id, kind, text })
      setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS)
    },
    info(text: string) {
      this.push('info', text)
    },
    success(text: string) {
      this.push('success', text)
    },
    error(text: string) {
      this.push('error', text)
    },
    dismiss(id: number) {
      this.messages = this.messages.filter((m) => m.id !== id)
    },
  },
})
