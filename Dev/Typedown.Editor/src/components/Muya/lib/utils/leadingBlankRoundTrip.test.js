window.chrome = {
  webview: {
    addEventListener() {},
    postMessage() {}
  }
}

jest.mock('services/remote/common', () => ({
  __esModule: true,
  default: {}
}))

jest.mock('services/common', () => ({
  __esModule: true,
  getTOC: () => ({ toc: [], cur: null }),
  getHtmlToc: () => '',
  matchString: () => []
}))

beforeAll(() => {
  Object.assign(window, {
    chrome: window.chrome
  })
})

const ContentState = require('../contentState').default
const ExportMarkdown = require('./exportMarkdown').default

function createContentState() {
  const muya = {
    options: {},
    eventCenter: {
      attachDOMEvent() {},
      subscribe() {}
    },
    container: {
      querySelectorAll() {
        return []
      }
    },
    blur() {}
  }

  return new ContentState(muya, {})
}

describe('leading blank line round trip', () => {
  test('preserves a leading blank line before a thematic break', () => {
    const markdown = '\n---\n\n# 文档目录\n'
    const contentState = createContentState()

    contentState.importMarkdown(markdown)

    const actual = new ExportMarkdown(
      contentState.getBlocks(),
      contentState.listIndentation,
      contentState.isGitlabCompatibilityEnabled
    ).generate()

    expect(actual).toBe(markdown)
  })
})
