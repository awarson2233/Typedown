import { Compartment, EditorState, type Extension } from '@codemirror/state';
import { EditorView, keymap } from '@codemirror/view';
import { defaultKeymap, history, historyKeymap, indentWithTab } from '@codemirror/commands';
import { syntaxHighlighting } from '@codemirror/language';
import { markdownSupport, type SyntaxOptions } from './syntax';
import { codeLanguages } from './syntax/codeLanguages';
import { inlineRevealExtension } from './decorations/inlinePlugin';
import { revealState } from './decorations/revealState';
import { blockField } from './widgets/blockField';
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
  setSourceMode(on: boolean): void;
  getText(): string;
  setText(text: string): void;
}

export function createEditor(opts: EditorOptions): TypedownEditor {
  const mode = new Compartment();
  const languageSupport = markdownSupport({ codeLanguages: opts.nestedCode ? codeLanguages : undefined, frontmatter: opts.frontmatter });
  const state = EditorState.create({
    doc: opts.doc,
    extensions: [
      languageSupport,
      history(),
      EditorView.lineWrapping,
      mode.of(opts.sourceMode ? [] : typoraLayer),
      // 全局 syntaxHighlighting 的 highlightTree 从文首逐个兄弟节点走到视口（markdown 的 Document 很扁平），
      // 1 MB 文档中部每键约 5 ms；不做嵌套代码解析时正文里只有 markdown 标记可高亮，显形装饰已覆盖，所以只在嵌套模式下开
      opts.nestedCode ? syntaxHighlighting(codeHighlightStyle) : [],
      keymap.of([...historyKeymap, indentWithTab, ...defaultKeymap]),
      EditorView.contentAttributes.of({ spellcheck: 'false', autocorrect: 'off', autocapitalize: 'off' }),
      opts.extensions ?? [],
    ],
  });
  const v = new EditorView({ state, parent: opts.parent });
  return {
    view: v,
    setSourceMode(on) { v.dispatch({ effects: mode.reconfigure(on ? [] : typoraLayer) }); },
    getText: () => v.state.doc.toString(),
    setText(text) { v.dispatch({ changes: { from: 0, to: v.state.doc.length, insert: text } }); },
  };
}
