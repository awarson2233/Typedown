import { Prec, type Extension } from '@codemirror/state';
import { EditorView, ViewPlugin, keymap, type Command } from '@codemirror/view';
import { syntaxTree } from '@codemirror/language';
import { iterateTopBlocks } from '../syntax';
import { renderMermaid } from '../../renderers/mermaid';
import { codeHighlight } from './codeHighlight';
import { fenceGuards } from './fenceGuards';
import { languageCompletion } from './languageCompletion';

/**
 * 块组件的配套行为（块组件本身在 widgets/blockField.ts）：
 * 代码高亮、围栏保护、语言补全、Ctrl+Enter 退出公式 / 图表的编辑态、明暗主题切换后重画 mermaid。
 */

/** 光标所在的顶层公式块或 mermaid 块的范围 */
function diagramBlockAt(view: EditorView, pos: number): { from: number; to: number } | null {
  let found: { from: number; to: number } | null = null;
  iterateTopBlocks(syntaxTree(view.state), pos, pos, n => {
    if (n.name === 'BlockMath') found = { from: n.from, to: n.to };
    else if (n.name === 'FencedCode') {
      const info = n.node.getChild('CodeInfo');
      if (info && /^mermaid$/i.test(view.state.sliceDoc(info.from, info.to).trim())) found = { from: n.from, to: n.to };
    }
  });
  return found;
}

/** Ctrl+Enter：离开公式块 / mermaid 的编辑态，光标放到块后的下一行（块在文末时补一个空行） */
export const exitDiagram: Command = view => {
  const sel = view.state.selection.main;
  if (!sel.empty) return false;
  const block = diagramBlockAt(view, sel.head);
  if (!block) return false;
  const doc = view.state.doc;
  const end = doc.lineAt(block.to);
  if (end.number < doc.lines) view.dispatch({ selection: { anchor: doc.line(end.number + 1).from }, scrollIntoView: true, userEvent: 'select' });
  else view.dispatch({ changes: { from: end.to, insert: '\n' }, selection: { anchor: end.to + 1 }, scrollIntoView: true, userEvent: 'input' });
  return true;
};

interface DiagramDom extends HTMLElement { tdSrc?: string }

/** mermaid 按明暗主题初始化；根元素 data-theme 变化后，把视图里已渲染的图按新主题重画 */
const mermaidThemeSync = ViewPlugin.fromClass(class {
  private readonly observer: MutationObserver | null;
  constructor(readonly view: EditorView) {
    const root = view.dom.ownerDocument.documentElement;
    this.observer = typeof MutationObserver === 'undefined' ? null : new MutationObserver(() => this.redraw());
    this.observer?.observe(root, { attributes: true, attributeFilter: ['data-theme'] });
  }
  private redraw() {
    for (const wrap of Array.from(this.view.dom.querySelectorAll<DiagramDom>('.cm-td-block.cm-td-mermaid'))) {
      const body = wrap.querySelector<HTMLElement>('.cm-td-block-body');
      const src = wrap.tdSrc;
      if (!body || src === undefined || !src.trim()) continue;
      const target = document.createElement('div');
      renderMermaid(target, src).then(() => { if (wrap.tdSrc === src) body.replaceChildren(...Array.from(target.childNodes)); }, () => undefined);
    }
  }
  destroy() { this.observer?.disconnect(); }
});

export const blockComponents: Extension = [
  codeHighlight,
  fenceGuards,
  languageCompletion,
  mermaidThemeSync,
  Prec.high(keymap.of([{ key: 'Mod-Enter', run: exitDiagram }])),
];
