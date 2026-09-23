import { KeyboardModifier, type KeyChord } from '../bridge/protocol';

/**
 * 快捷键桥（规则照搬旧页面 services/keyboard.ts）。
 * WinUI 的 WebView2 独占键盘输入，焦点在编辑器里时 XAML 收不到 KeyDown；宿主把当前认领的全部和弦经 view.keymap 下发，
 * 页面在捕获阶段比对，命中就 preventDefault 并以 view.shortcut 回报，由宿主执行命令。
 * 快捷键是用户可配置的，拦截范围只由宿主下发的表决定，页面不硬编码和弦。
 */

/** KeyboardEvent 里本桥用到的字段（单测用普通对象构造） */
export interface KeyLike {
  key: string;
  keyCode: number;
  ctrlKey: boolean;
  altKey: boolean;
  shiftKey: boolean;
  metaKey: boolean;
  isComposing: boolean;
  defaultPrevented: boolean;
  repeat?: boolean;
}

export function readModifiers(e: KeyLike): number {
  let m = KeyboardModifier.None as number;
  if (e.ctrlKey) m |= KeyboardModifier.Control;
  if (e.altKey) m |= KeyboardModifier.Menu;
  if (e.shiftKey) m |= KeyboardModifier.Shift;
  if (e.metaKey) m |= KeyboardModifier.Windows;
  return m;
}

const MODIFIER_KEYS = new Set(['Shift', 'Control', 'Alt', 'Meta']);

/**
 * 这次 keydown 是否完全绕过快捷键桥（不拦截、不上报）。
 * 刻意不过滤 event.repeat：宿主自己的 XAML KeyDown 也不过滤，焦点在编辑器内外的行为必须一致；
 * 放行长按重复还会让 Ctrl+Z 漏给 CM6 自己的撤销，宿主认领的和弦必须一次都不落到页面上。
 */
export function shouldBypass(e: KeyLike): boolean {
  // 输入法组字期间浏览器发 keyCode 229 的合成按键，拦下会打断中文输入；isComposing 在部分内核上缺失，两个都查
  if (e.isComposing || e.keyCode === 229) return true;
  // 更靠外层的捕获监听已经认领过这次按键
  if (e.defaultPrevented) return true;
  // 单按修饰键不构成和弦；设置里万一存进了退化的注册项，这里兜住，避免吞掉所有 Ctrl
  if (MODIFIER_KEYS.has(e.key)) return true;
  return false;
}

const chordId = (key: number, modifiers: number) => `${key}:${modifiers}`;

export class Keymap {
  private chords = new Set<string>();

  set(chords: readonly KeyChord[] | null | undefined) {
    this.chords = new Set((chords ?? []).filter(c => typeof c?.key === 'number' && typeof c?.modifiers === 'number').map(c => chordId(c.key, c.modifiers)));
  }

  get size() { return this.chords.size; }

  /** 命中时返回要回报的和弦，否则 null（含所有放行的情形） */
  match(e: KeyLike): KeyChord | null {
    if (shouldBypass(e)) return null;
    const modifiers = readModifiers(e);
    return this.chords.has(chordId(e.keyCode, modifiers)) ? { key: e.keyCode, modifiers } : null;
  }
}

/** 在捕获阶段挂监听；返回卸载函数 */
export function installShortcutBridge(target: Pick<Document, 'addEventListener' | 'removeEventListener'>, keymap: Keymap, onShortcut: (chord: KeyChord) => void): () => void {
  const listener = (e: KeyboardEvent) => {
    const chord = keymap.match(e);
    if (!chord) return;
    // 宿主认领了这个和弦：压掉浏览器与 CM6 的默认行为，由宿主侧执行命令
    e.preventDefault();
    e.stopPropagation();
    onShortcut(chord);
  };
  target.addEventListener('keydown', listener as EventListener, true);
  return () => target.removeEventListener('keydown', listener as EventListener, true);
}
