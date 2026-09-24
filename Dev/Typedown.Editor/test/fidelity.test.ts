// @vitest-environment jsdom
import { beforeAll, describe, expect, it } from 'vitest';
import { readdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { EditorSelection } from '@codemirror/state';
import { ensureSyntaxTree } from '@codemirror/language';
import { undo } from '@codemirror/commands';
import { createEditor, type TypedownEditor } from '../src/editor/createEditor';
import { openCell, Leave } from '../src/editor/widgets/cellEditor';
import type { TableDom } from '../src/editor/widgets/tableCellRender';
import { sampleDoc, IME_DOC } from '../src/dev/sampleDocs';
import { allExamples, label } from './spec';

/**
 * 字节保真（webview-wysiwyg-engine.md 第 8 节测试分层）：
 * - 载入后立即取回 ≡ 原文；把光标依次放到每一行（让显形与块组件的两态都切换一遍）后仍 ≡ 原文；
 * - 编辑一处后只有该处变化。
 * 语料：CommonMark 0.31.2 与 GFM 0.29 官方用例、示例文档、仓库 docs/ 下的 markdown。
 * 视图跑在 jsdom 里（没有布局，但装饰、widget 的 toDOM、DOM 观察器都真实运行）。
 */

beforeAll(() => {
  // jsdom 没有布局相关 API，CM6 的测量阶段需要它们存在
  const rect = () => ({ left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0, toJSON() {} }) as DOMRect;
  const rects = () => Object.assign([], { item: () => null }) as unknown as DOMRectList;
  Range.prototype.getBoundingClientRect = rect;
  Range.prototype.getClientRects = rects;
  Element.prototype.getClientRects = rects;
});

function mount(doc: string): TypedownEditor {
  const parent = document.createElement('div');
  document.body.appendChild(parent);
  const ed = createEditor({ doc, parent });
  ensureSyntaxTree(ed.view.state, doc.length, 1e9);
  ed.view.dispatch({}); // 让语法树与块组件覆盖全文
  return ed;
}

function walkLines(ed: TypedownEditor) {
  const { view } = ed;
  for (let l = 1; l <= view.state.doc.lines; l++) {
    const line = view.state.doc.line(l);
    view.dispatch({ selection: EditorSelection.cursor(line.from + (line.length >> 1)) });
  }
}

const repoDocs = (() => {
  const dir = resolve(import.meta.dirname, '../../../docs');
  try {
    return readdirSync(dir).filter(f => f.endsWith('.md')).map(f => ({ name: `docs/${f}`, text: readFileSync(resolve(dir, f), 'utf8').replace(/\r\n/g, '\n') }));
  } catch { return []; }
})();

const corpus = [
  ...allExamples.map(e => ({ name: label(e), text: e.markdown })),
  ...['small', 'rich', 'showcase'].map(n => ({ name: `sample ${n}`, text: sampleDoc(n) })),
  { name: 'ime', text: IME_DOC },
  ...repoDocs,
];

// 语料有 1300 多篇，每篇都建一次视图；并行跑其他测试文件时会超过默认的 5 s
describe('字节保真', { timeout: 120000 }, () => {
  it(`语料 ${corpus.length} 篇：载入与逐行移动光标后取回 ≡ 原文`, () => {
    const bad: string[] = [];
    for (const { name, text } of corpus) {
      const ed = mount(text);
      if (ed.getText() !== text) bad.push(`${name}: 载入后不同`);
      walkLines(ed);
      if (ed.getText() !== text) bad.push(`${name}: 移动光标后不同`);
      ed.view.destroy();
    }
    expect(bad).toEqual([]);
  });

  it('编辑一处后只有该处变化（每篇在一个确定的伪随机位置插入与删除）', () => {
    const bad: string[] = [];
    corpus.forEach(({ name, text }, i) => {
      const ed = mount(text);
      const p = text.length ? (i * 7919) % (text.length + 1) : 0;
      ed.view.dispatch({ changes: { from: p, insert: 'X' }, selection: { anchor: p + 1 }, userEvent: 'input.type' });
      if (ed.getText() !== text.slice(0, p) + 'X' + text.slice(p)) bad.push(`${name}: 插入`);
      ed.view.dispatch({ changes: { from: p, to: p + 1 }, userEvent: 'delete.backward' });
      if (ed.getText() !== text) bad.push(`${name}: 删除后未复原`);
      ed.view.destroy();
    });
    expect(bad).toEqual([]);
  });

  it('单元格内编辑只改该格字节（每张顶层表格逐格在格末输入再撤销）', () => {
    const bad: string[] = [];
    let cells = 0;
    for (const { name, text } of corpus) {
      if (!text.includes('|')) continue;
      const ed = mount(text);
      const { view } = ed;
      const wraps = [...view.contentDOM.querySelectorAll<TableDom>('.cm-td-table-wrap')];
      for (let w = 0; w < wraps.length; w++) {
        const grid = wraps[w].tdCells ?? [];
        grid.forEach((line, r) => line.forEach((_span, c) => {
          // 每次都重新取外框：前一格撤销后表格可能重建
          const wrap = view.contentDOM.querySelectorAll<TableDom>('.cm-td-table-wrap')[w];
          const s = wrap && openCell(view, wrap, r, c, Infinity);
          if (!s) { bad.push(`${name} t${w} r${r}c${c}: 打不开`); return; }
          const cell = wrap.tdTable!.model;
          const range = [cell.header, ...cell.body][r].cells[c];
          if (!range) { s.close(Leave.Stay); return; } // 缺的单元格要补竖线，不在这条性质里
          const at = view.posAtDOM(wrap) + range.to;
          s.nested.dispatch({ changes: { from: s.nested.state.doc.length, insert: 'Z' }, userEvent: 'input.type' });
          cells++;
          if (ed.getText() !== text.slice(0, at) + 'Z' + text.slice(at)) bad.push(`${name} t${w} r${r}c${c}: 输入`);
          s.close(Leave.Stay);
          undo(view);
          if (ed.getText() !== text) bad.push(`${name} t${w} r${r}c${c}: 撤销后未复原`);
        }));
      }
      ed.view.destroy();
    }
    expect(cells).toBeGreaterThan(100);
    expect(bad).toEqual([]);
  });

  it('CRLF 由宿主处理：引擎内只有 \\n（CM6 按行分隔符拆分，取回时统一为 \\n）', () => {
    const ed = mount('a\r\nb\r\n');
    expect(ed.getText()).toBe('a\nb\n');
    ed.view.destroy();
  });
});
