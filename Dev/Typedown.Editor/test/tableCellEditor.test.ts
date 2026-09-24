// @vitest-environment jsdom
import { afterEach, beforeAll, describe, expect, it } from 'vitest';
import { EditorView } from '@codemirror/view';
import { ensureSyntaxTree } from '@codemirror/language';
import { undo, redo } from '@codemirror/commands';
import { createEditor, type TypedownEditor } from '../src/editor/createEditor';
import { cellSpecs, renderCell, CELL_CLASS, type TableDom } from '../src/editor/widgets/tableCellRender';
import { cellSession, openCell, Leave } from '../src/editor/widgets/cellEditor';
import { cellSourceFixes, toCellSource } from '../src/editor/commands/tableCells';
import { buildInlineSpecs } from '../src/editor/decorations/inlineSpecs';
import { fullTree, stateOf } from './helpers';

/**
 * 表格单元格（B1）：非焦点单元格按正文的行内规则渲染；焦点单元格的嵌套视图把编辑映射回该格的源码区间，
 * 撤销走主视图的历史，外部变化同步回嵌套视图。视图跑在 jsdom 里（没有布局）。
 */

beforeAll(() => {
  const rect = () => ({ left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0, toJSON() {} }) as DOMRect;
  const rects = () => Object.assign([], { item: () => null }) as unknown as DOMRectList;
  Range.prototype.getBoundingClientRect = rect;
  Range.prototype.getClientRects = rects;
  Element.prototype.getClientRects = rects;
});

const editors: TypedownEditor[] = [];
afterEach(() => {
  for (const ed of editors.splice(0)) { cellSession(ed.view)?.close(Leave.Stay); ed.view.destroy(); ed.view.dom.parentElement?.remove(); }
});

function mount(doc: string): TypedownEditor {
  const parent = document.createElement('div');
  document.body.appendChild(parent);
  const ed = createEditor({ doc, parent });
  ensureSyntaxTree(ed.view.state, doc.length, 1e9);
  ed.view.dispatch({});
  editors.push(ed);
  return ed;
}

const wrapOf = (view: EditorView, i = 0) => view.contentDOM.querySelectorAll<TableDom>('.cm-td-table-wrap')[i];
const cellText = (view: EditorView, r: number, c: number, i = 0) => wrapOf(view, i).tdCells![r][c].textContent;
/** 在嵌套视图里模拟输入（与键入同样的用户事件，经过输入过滤器） */
const type = (view: EditorView, text: string, at?: number) => {
  const pos = at ?? view.state.selection.main.head;
  view.dispatch({ changes: { from: pos, insert: text }, selection: { anchor: pos + text.length }, userEvent: 'input.type' });
};

describe('非焦点单元格的渲染', () => {
  it('行内格式按正文的规则渲染，标记隐藏', () => {
    const ed = mount('| h |\n|---|\n| **粗** `码` [链](u) ~~删~~ a \\| b :smile: |\n');
    const span = wrapOf(ed.view).tdCells![1][0];
    expect(span.querySelector('.cm-td-strong')?.textContent).toBe('粗');
    expect(span.querySelector('.cm-td-code-inline')?.textContent).toBe('码');
    expect(span.querySelector('.cm-td-link')?.textContent).toBe('链');
    expect(span.querySelector('.cm-td-del')?.textContent).toBe('删');
    expect(span.querySelector('.cm-td-emoji')).not.toBeNull();
    expect(span.textContent).toBe('粗 码 链 删 a | b 😄');
  });

  it('逐类行内格式：粗体、行内代码（含转义竖线）、链接、图片、公式、删除线、高亮、emoji、混合嵌套', () => {
    const ed = mount('x\n');
    const span = document.createElement('span');
    const cases: [string, string, string | null][] = [
      ['**粗体**', '.cm-td-strong', '粗体'],
      ['`a \\| b`', '.cm-td-code-inline', 'a | b'],
      ['[链接](https://e.com "t")', '.cm-td-link', '链接'],
      ['![图](p.png)', '.cm-td-image', null],
      ['$x^2$', '.cm-td-math-inline', null],
      ['~~删除~~', '.cm-td-del', '删除'],
      ['==高亮==', '.cm-td-highlight', '高亮'],
      [':rocket:', '.cm-td-emoji', '🚀'],
      ['***粗斜***', '.cm-td-strong .cm-td-em, .cm-td-em .cm-td-strong', '粗斜'],
      ['**粗 `码` [链](u)**', '.cm-td-strong .cm-td-code-inline', '码'],
      ['[**粗链**](u)', '.cm-td-link .cm-td-strong', '粗链'],
      ['~~删 *斜*~~', '.cm-td-del .cm-td-em', '斜'],
    ];
    for (const [src, sel, text] of cases) {
      renderCell(span, src, ed.view);
      const el = span.querySelector(sel);
      expect(el, src).not.toBeNull();
      if (text !== null) expect(el!.textContent, src).toBe(text);
    }
    // 行内代码里的 `\|` 是表格一级的转义：渲染成竖线；代码外的 `\|` 是普通转义
    renderCell(span, 'b `\\|` az **\\|** im', ed.view);
    expect(span.textContent).toBe('b | az | im');
  });

  it('决策与正文段落里的同一段文字相同（规则只有一份）', () => {
    const inline = '**a** `b` [c](d "t") *e* $x$ ==f== <kbd>K</kbd> ![i](p.png)';
    const cell = cellSpecs(inline).filter(s => s.kind !== 'line');
    const state = stateOf(inline + '\n');
    const body = buildInlineSpecs(state.doc, fullTree(state), { from: 0, to: inline.length }, []).filter(s => s.kind !== 'line');
    expect(cell).toEqual(body);
  });

  it('单元格里的块级写法按文字显示（GFM 单元格只有行内内容）', () => {
    const span = document.createElement('span');
    const ed = mount('x\n');
    for (const src of ['# 标题', '- 项', '> 引', '1. 一', '---']) {
      renderCell(span, src, ed.view);
      expect(span.textContent).toBe(src);
    }
  });
});

describe('单元格源码修正', () => {
  it('未转义的竖线补反斜杠，换行变空格，已转义的不动', () => {
    expect(toCellSource('a|b')).toBe('a\\|b');
    expect(toCellSource('a\\|b')).toBe('a\\|b');
    expect(toCellSource('a\\\\|b')).toBe('a\\\\\\|b');
    expect(toCellSource('a\nb\r\nc')).toBe('a b c');
    expect(cellSourceFixes('ok')).toEqual([]);
  });
});

describe('嵌套视图：编辑映射回单元格区间', () => {
  const doc = '前\n\n|  姓名  | 城市|\n|---|---:|\n| 张三   |北京 |\n|李四|  上海   |\n\n后\n';

  it('输入只改该格字节，竖线转义；撤销、重做走主视图的历史', () => {
    const ed = mount(doc);
    const s = openCell(ed.view, wrapOf(ed.view), 1, 0, Infinity)!;
    expect(s.text).toBe('张三');
    type(s.nested, '丰|');
    const after = doc.replace('| 张三   |', '| 张三丰\\|   |');
    expect(ed.getText()).toBe(after);
    expect(s.text).toBe('张三丰\\|');
    undo(ed.view);
    expect(ed.getText()).toBe(doc);
    expect(s.text).toBe('张三');
    redo(ed.view);
    expect(ed.getText()).toBe(after);
    expect(s.text).toBe('张三丰\\|');
  });

  it('组字期间不提交，结束后一次提交', () => {
    const ed = mount(doc);
    const s = openCell(ed.view, wrapOf(ed.view), 2, 1, Infinity)!;
    let composing = true;
    Object.defineProperty(s.nested, 'composing', { get: () => composing, configurable: true });
    s.nested.dispatch({ changes: { from: 2, insert: 'sh' }, userEvent: 'input.type.compose' });
    s.nested.dispatch({ changes: { from: 2, to: 4, insert: '市' }, userEvent: 'input.type.compose' });
    expect(ed.getText()).toBe(doc);
    composing = false;
    s.commit();
    expect(ed.getText()).toBe(doc.replace('|  上海   |', '|  上海市   |'));
  });

  it('外部变化：别处的编辑不打断，改到这一格时同步进嵌套视图', () => {
    const ed = mount(doc);
    const s = openCell(ed.view, wrapOf(ed.view), 1, 1, Infinity)!;
    ed.view.dispatch({ changes: { from: 0, insert: '最' } });
    type(s.nested, '市');
    expect(ed.getText()).toBe('最' + doc.replace('|北京 |', '|北京市 |'));
    const at = ed.getText().indexOf('北京市');
    ed.view.dispatch({ changes: { from: at, to: at + 2, insert: '南' } });
    expect(s.text).toBe('南市');
    expect(cellSession(ed.view)).toBe(s);
  });

  it('缺的单元格：第一次输入补齐竖线，表格重建后嵌套视图挪进新表格', () => {
    const src = '| a | b | c |\n|---|---|---|\n| 1 |\n';
    const ed = mount(src);
    const s = openCell(ed.view, wrapOf(ed.view), 1, 2, 0)!;
    type(s.nested, 'z');
    expect(ed.getText()).toBe('| a | b | c |\n|---|---|---|\n| 1 |  | z |\n');
    expect(s.closed).toBe(false);
    expect(s.host.isConnected).toBe(true);
    type(s.nested, 'y');
    expect(ed.getText()).toBe('| a | b | c |\n|---|---|---|\n| 1 |  | zy |\n');
  });

  it('Tab 跳到下一格、离开后恢复静态渲染；撤销到格外时回到主视图', () => {
    const ed = mount(doc);
    let s = openCell(ed.view, wrapOf(ed.view), 0, 0, Infinity)!;
    expect(s.moveBy(1, Infinity)).toBe(true);
    s = cellSession(ed.view)!;
    expect([s.row, s.col]).toEqual([0, 1]);
    expect(wrapOf(ed.view).querySelectorAll('.cm-td-cell-active')).toHaveLength(1);
    expect(cellText(ed.view, 0, 0)).toBe('姓名');
    s.close(Leave.TableEnd);
    expect(wrapOf(ed.view).querySelectorAll('.cm-td-cell-active')).toHaveLength(0);
    expect(cellText(ed.view, 0, 1)).toBe('城市');
    // 格外先改一处，再进格子撤销：撤销的是格外那一步，会话结束、选区回到格外
    ed.view.dispatch({ changes: { from: doc.length - 2, insert: '！' }, userEvent: 'input.type' });
    s = openCell(ed.view, wrapOf(ed.view), 1, 0, 0)!;
    undo(ed.view);
    expect(ed.getText()).toBe(doc);
    expect(s.closed).toBe(true);
  });

  it('单元格内容只在源码变了时重画（焦点格不被 updateDOM 改写）', () => {
    const ed = mount(doc);
    const wrap = wrapOf(ed.view);
    const other = wrap.tdCells![2][0].firstChild;
    const s = openCell(ed.view, wrap, 1, 0, Infinity)!;
    type(s.nested, 'x');
    expect(wrap.isConnected).toBe(true);
    expect(wrap.tdCells![2][0].firstChild).toBe(other);
    expect(wrap.tdCells![1][0].querySelector('.cm-editor')).toBe(s.nested.dom);
    expect(wrap.querySelectorAll(`.${CELL_CLASS}`)).toHaveLength(6);
  });
});
