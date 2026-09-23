import BaseScrollFloat from '../baseScrollFloat'
import { patch, h } from '../../parser/render/snabbdom'
import { search } from '../../prism/index'

import './index.css'

const defaultOptions = {
  placement: 'bottom-start',
  modifiers: {
    offset: {
      offset: '0, 0'
    }
  },
  showArrow: false
}

// file-icons 在模块求值时就要建全部图标规则，外加 84 KB CSS 与 5 个字体，只为这个浮层画语言图标；
// 改为浮层第一次打开时加载，加载完成前图标位留空，加载后重绘一次。
let fileIcons = null
let fileIconsLoading = null
const loadFileIcons = () => {
  if (!fileIconsLoading) {
    fileIconsLoading = import(/* webpackChunkName: "file-icons" */ '../fileIcons').then(m => {
      fileIcons = m.default
    }, err => {
      fileIconsLoading = null
      throw err
    })
  }
  return fileIconsLoading
}

class CodePicker extends BaseScrollFloat {
  static pluginName = 'codePicker'

  constructor (muya, options = {}) {
    const name = 'ag-list-picker'
    const opts = Object.assign({}, defaultOptions, options)
    super(muya, name, opts)
    this.renderArray = []
    this.oldVnode = null
    this.activeItem = null
    this.listen()
  }

  listen () {
    super.listen()
    const { eventCenter } = this.muya
    eventCenter.subscribe('muya-code-picker', ({ reference, lang, cb }) => {
      const modes = search(lang)
      if (modes.length && reference) {
        if (!fileIcons) {
          loadFileIcons().then(() => {
            if (this.status) this.render()
          }, err => console.error(err))
        }
        this.show(reference, cb)
        this.renderArray = modes
        this.activeItem = modes[0]
        this.render()
      } else {
        this.hide()
      }
    })
  }

  render () {
    const { renderArray, oldVnode, scrollElement, activeItem } = this
    let children = renderArray.map(item => {
      let iconClassNames

      if (item.name && fileIcons) {
        iconClassNames = fileIcons.getClassByLanguage(item.name)
      }

      // Because `markdown mode in Codemirror` don't have extensions.
      // if still can not get the className, add a common className 'atom-icon light-cyan'
      if (!iconClassNames && fileIcons) {
        iconClassNames = item.name === 'markdown' ? fileIcons.getClassByName('fackname.md') : 'atom-icon light-cyan'
      }
      const iconSelector = 'span' + (iconClassNames || '').split(/\s/).filter(Boolean).map(s => `.${s}`).join('')
      const icon = h('div.icon-wrapper', h(iconSelector))
      const text = h('div.language', item.name)
      const selector = activeItem === item ? 'li.item.active' : 'li.item'
      return h(selector, {
        dataset: {
          label: item.name
        },
        on: {
          click: () => {
            this.selectItem(item)
          }
        }
      }, [icon, text])
    })

    if (children.length === 0) {
      children = h('div.no-result', 'No result')
    }
    const vnode = h('ul', children)

    if (oldVnode) {
      patch(oldVnode, vnode)
    } else {
      patch(scrollElement, vnode)
    }
    this.oldVnode = vnode
  }

  getItemElement (item) {
    const { name } = item
    return this.floatBox.querySelector(`[data-label="${name}"]`)
  }
}

export default CodePicker
