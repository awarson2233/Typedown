import transport from './transport'

// WinUI3 的 WebView2 控件独占键盘输入，焦点落在编辑器里时 XAML 永远收不到 KeyDown，
// 宿主的快捷键体系就整体失效。这里补上唯一的回程：宿主把当前注册的快捷键全集下发到页面，
// 页面在捕获阶段比对并拦下命中的和弦，再原样回传给宿主补触发。
// 快捷键是用户可配置的，所以拦截范围必须由宿主下发的表决定，不能在页面里硬编码和弦。

/** 与 Typedown.Core.Models.KeyboardModifiers 的位定义保持一致 */
const Modifier = {
    None: 0,
    Control: 1,
    Menu: 2,
    Shift: 4,
    Windows: 8,
}

interface Chord {
    key: number
    modifiers: number
}

const chordId = (key: number, modifiers: number) => `${key}:${modifiers}`

let registered = new Set<string>()

transport.addListener<{ shortcuts: Chord[] }>('SetShortcuts', ({ shortcuts }) => {
    registered = new Set((shortcuts ?? []).map(({ key, modifiers }) => chordId(key, modifiers)))
})

const readModifiers = (event: KeyboardEvent) => {
    let modifiers = Modifier.None
    if (event.ctrlKey) modifiers |= Modifier.Control
    if (event.altKey) modifiers |= Modifier.Menu
    if (event.shiftKey) modifiers |= Modifier.Shift
    if (event.metaKey) modifiers |= Modifier.Windows
    return modifiers
}

const MODIFIER_KEYS = new Set(['Shift', 'Control', 'Alt', 'Meta'])

/**
 * 判断这次 keydown 是否应当完全跳过快捷键桥，直接交回页面处理。
 * 返回 true 表示不拦截、不上报。
 *
 * 注意这里刻意不过滤 event.repeat：宿主自己的 XAML KeyDown 通道也不过滤，
 * 焦点在编辑器内外的行为必须一致；更关键的是，一旦放行长按重复，
 * Ctrl+Z 就会漏给浏览器的 contenteditable 原生撤销，撤出一份宿主 History 不知道的正文。
 * 宿主认领的和弦必须一次都不落到页面上。
 */
const shouldBypass = (event: KeyboardEvent): boolean => {
    // 输入法组字期间浏览器会发 keyCode 229 的合成按键，拦下来会直接打断中文输入。
    // isComposing 在部分内核上缺失，所以两个条件都查。
    if (event.isComposing || event.keyCode === 229) {
        return true
    }

    // 更靠外层的捕获监听已经认领过这次按键。
    if (event.defaultPrevented) {
        return true
    }

    // 单按修饰键本身不构成和弦。万一设置里存进了退化的注册项，这里兜住，避免吞掉所有 Ctrl。
    if (MODIFIER_KEYS.has(event.key)) {
        return true
    }

    return false
}

document.addEventListener('keydown', event => {
    if (shouldBypass(event)) {
        return
    }

    const modifiers = readModifiers(event)
    if (!registered.has(chordId(event.keyCode, modifiers))) {
        return
    }

    // 宿主认领了这个和弦：压掉浏览器与 Muya 的默认行为，由宿主侧的命令执行。
    event.preventDefault()
    event.stopPropagation()
    transport.postMessageNoDiff('KeyDown', { key: event.keyCode, modifiers })
}, true)

export { }
