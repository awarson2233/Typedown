import { Compartment, EditorState, type Extension } from '@codemirror/state';
import { EditorView, keymap } from '@codemirror/view';
import { defaultKeymap, history, historyKeymap, indentWithTab } from '@codemirror/commands';
import { syntaxHighlighting } from '@codemirror/language';
import { markdownSupport, type SyntaxOptions } from './syntax';
import { codeLanguages } from './syntax/codeLanguages';
import { inlineRevealExtension } from './decorations/inlinePlugin';
import { revealState } from './decorations/revealState';
import { blockField } from './widgets/blockField';
import { outlineField } from './state/outline';
import { codeHighlightStyle } from './highlight';

export interface EditorOptions {
  doc: string;
  parent: HTMLElement;
  /** 源码模式：关掉显形与块组件（Compartment 重配，光标与历史保留） */
  sourceMode?: boolean;
  /** 围栏代码按语言嵌套解析与高亮（parseMixed，语言按需加载）；默认关，见 syntax/index.ts 与性能对照 */
  nestedCode?: boolean;
  /** front matter 解析方式，见 SyntaxOptions */
  frontmatter?: SyntaxOptions['frontmatter'];
  /** 制表符宽度，默认 4 */
  tabSize?: number;
  /** 拼写检查，默认关 */
  spellcheck?: boolean;
  extensions?: Extension[];
}

/** 显形层（行内显形 + 块组件）。单独导出，单测与源码模式切换共用。 */
export const typoraLayer: Extension = [revealState, inlineRevealExtension, blockField];

/** 与视图无关、测试可直接用的基础扩展（语法 + 历史）。 */
export function coreExtensions(): Extension[] {
  return [markdownSupport(), history(), EditorView.lineWrapping];
}

export interface TypedownEditor {
  view: EditorView;
  /** 以当前配置（源码模式、制表符宽度、拼写检查）建一个新状态，doc.load 用 */
  createState(doc: string, selection?: { anchor: number; head?: number }): EditorState;
  setSourceMode(on: boolean): void;
  readonly sourceMode: boolean;
  setTabSize(size: number): void;
  setSpellcheck(on: boolean): void;
  /** 清空撤销历史，以当前正文为新的起点 */
  clearHistory(): void;
  getText(): string;
  setText(text: string): void;
}

export function createEditor(opts: EditorOptions): TypedownEditor {
  const mode = new Compartment(), hist = new Compartment(), tab = new Compartment(), spell = new Compartment();
  let sourceMode = !!opts.sourceMode, tabSize = opts.tabSize ?? 4, spellcheck = !!opts.spellcheck;
  const languageSupport = markdownSupport({ codeLanguages: opts.nestedCode ? codeLanguages : undefined, frontmatter: opts.frontmatter });
  const spellAttrs = (on: boolean) => EditorView.contentAttributes.of({ spellcheck: on ? 'true' : 'false' });
  // 每次建状态都按当前配置取值：doc.load 换状态后，源码模式等设置不能退回初始值
  const extensions = (): Extension[] => [
    languageSupport,
    hist.of(history()),
    EditorView.lineWrapping,
    // 大纲与标题 id：行首扫描器按改动增量维护，不依赖语法树（源码模式也要）
    outlineField,
    mode.of(sourceMode ? [] : typoraLayer),
    // 全局 syntaxHighlighting 的 highlightTree 从文首逐个兄弟节点走到视口（markdown 的 Document 很扁平），
    // 1 MB 文档中部每键约 5 ms；不做嵌套代码解析时正文里只有 markdown 标记可高亮，显形装饰已覆盖，所以只在嵌套模式下开
    opts.nestedCode ? syntaxHighlighting(codeHighlightStyle) : [],
    keymap.of([...historyKeymap, indentWithTab, ...defaultKeymap]),
    EditorView.contentAttributes.of({ autocorrect: 'off', autocapitalize: 'off' }),
    spell.of(spellAttrs(spellcheck)),
    tab.of(EditorState.tabSize.of(tabSize)),
    opts.extensions ?? [],
  ];
  const v = new EditorView({ state: EditorState.create({ doc: opts.doc, extensions: extensions() }), parent: opts.parent });
  return {
    view: v,
    createState: (doc, selection) => EditorState.create({ doc, selection, extensions: extensions() }),
    setSourceMode(on) {
      if (on === sourceMode) return;
      sourceMode = on;
      v.dispatch({ effects: mode.reconfigure(on ? [] : typoraLayer) });
    },
    get sourceMode() { return sourceMode; },
    setTabSize(size) {
      if (size === tabSize || !(size > 0)) return;
      tabSize = size;
      v.dispatch({ effects: tab.reconfigure(EditorState.tabSize.of(size)) });
    },
    setSpellcheck(on) {
      if (on === spellcheck) return;
      spellcheck = on;
      v.dispatch({ effects: spell.reconfigure(spellAttrs(on)) });
    },
    clearHistory() {
      // 先摘掉 history 再装回：历史状态字段随配置移除而丢弃，重新装上时从空历史开始
      v.dispatch({ effects: hist.reconfigure([]) });
      v.dispatch({ effects: hist.reconfigure(history()) });
    },
    getText: () => v.state.doc.toString(),
    setText(text) { v.dispatch({ changes: { from: 0, to: v.state.doc.length, insert: text } }); },
  };
}
