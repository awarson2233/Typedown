import React from 'react'
import { act, render } from '@testing-library/react'

var mockListeners = new Map<string, (args: any) => void>()
var mockPostMessage = jest.fn()

jest.mock('services/transport', () => ({
  __esModule: true,
  default: {
    addListener: (name: string, handler: (args: any) => void) => {
      mockListeners.set(name, handler)
      return () => mockListeners.delete(name)
    },
    postMessage: (...args: any[]) => mockPostMessage(...args)
  }
}))

jest.mock('services/menuState', () => ({
  __esModule: true,
  createApplicationMenuState: () => ({})
}))

jest.mock('components/Muya/lib/ui/tablePicker', () => ({ __esModule: true, default: class {} }))
jest.mock('components/Muya/lib/ui/codePicker', () => ({ __esModule: true, default: class {} }))
jest.mock('components/Muya/lib/ui/emojiPicker', () => ({ __esModule: true, default: class {} }))
jest.mock('components/Muya/lib/ui/imageSelector', () => ({ __esModule: true, default: class {} }))
jest.mock('components/Muya/lib/ui/imageToolbar', () => ({ __esModule: true, default: class {} }))
jest.mock('components/Muya/lib/ui/linkTools', () => ({ __esModule: true, default: class {} }))
jest.mock('components/Muya/lib/ui/tableTools', () => ({ __esModule: true, default: class {} }))
jest.mock('components/Muya/lib/ui/footnoteTool', () => ({ __esModule: true, default: class {} }))
jest.mock('components/Muya/lib/ui/frontMenu', () => ({ __esModule: true, default: class {} }))

var mockLatestMuyaInstance: any

jest.mock('components/Muya/lib', () => {
  return {
    __esModule: true,
    default: class MockMuya {
      static use() {}

      options: Record<string, unknown>
      handlers: Record<string, Function>
      clipboard: Record<string, Function>

      constructor() {
        this.options = {}
        this.handlers = {}
        this.clipboard = {
          copy() {},
          cut() {},
          paste() {}
        }
        mockLatestMuyaInstance = this
      }

      on(name: string, handler: Function) {
        this.handlers[name] = handler
      }

      setMarkdown(markdown: string) {
        const normalized = markdown.replace(/^\n/, '')
        setTimeout(() => {
          this.handlers.contentChange?.({
            markdown: normalized,
            wordCount: 0,
            cursor: undefined,
            toc: { toc: [], cur: null }
          })

          this.handlers.contentChange?.({
            markdown: normalized,
            wordCount: 0,
            cursor: undefined,
            toc: { toc: [], cur: null }
          })
        }, 0)
      }

      getSelection() {
        return {
          cursorCoords: { y: 0 }
        }
      }

      search() {}
      setFocusMode() {}
      setFont() {}
      setTabSize() {}
      updateParagraph() {}
      insertParagraph() {}
      deleteParagraph() {}
      duplicate() {}
      format() {}
      delete() {}
      selectAll() {}
      createTable() {}
      insertImage() {}
      find() {}
      replace() {}
      focus() {}
      destroy() {}
    }
  }
})

import MuyaEditor from './index'

describe('MuyaEditor programmatic load', () => {
  beforeEach(() => {
    jest.useFakeTimers()
    mockPostMessage.mockClear()
    mockListeners.clear()
    mockLatestMuyaInstance = undefined
    window.scrollTo = jest.fn()
    window.scrollBy = jest.fn()
  })

  afterEach(() => {
    jest.runOnlyPendingTimers()
    jest.useRealTimers()
  })

  test('does not forward programmatic contentChange events as markdown edits', () => {
    const onMarkdownChange = jest.fn()
    const onCursorChange = jest.fn()
    const onSearchArgChange = jest.fn()
    const scrollTopRef = { current: 0 }

    render(
      <MuyaEditor
        markdown={'\n---\n\n# 文档目录\n'}
        cursor={undefined}
        options={{}}
        searchOpen={0}
        searchArg={undefined}
        scrollTopRef={scrollTopRef}
        onMarkdownChange={onMarkdownChange}
        onCursorChange={onCursorChange}
        onSearchArgChange={onSearchArgChange}
      />
    )

    expect(mockLatestMuyaInstance).toBeTruthy()

    act(() => {
      jest.runAllTimers()
    })

    expect(onMarkdownChange).not.toHaveBeenCalled()
  })
})
