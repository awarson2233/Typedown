// @vitest-environment jsdom
import { afterEach, beforeAll, describe, expect, it } from 'vitest';
import { Text } from '@codemirror/state';
import { EditorView, runScopeHandlers } from '@codemirror/view';
import { ensureSyntaxTree } from '@codemirror/language';
import { undo } from '@codemirror/commands';
import { createEditor, type TypedownEditor } from '../src/editor/createEditor';
import { parseTable } from '../src/editor/commands/tableCells';
import { clearCells, subTableMarkdown, tableDeletion } from '../src/editor/commands/tableSelect';
import { cellSession, openCell, Leave } from '../src/editor/widgets/cellEditor';
import { selectionText, tableSelectionField, trackCellDrag } from '../src/editor/widgets/tableSelection';
import type { TableDom } from '../src/editor/widgets/tableCellRender';

/**
 * 表格块选择（B2）：整表选中与矩形多选的源码替换、复制文本；方向键移入、Ctrl+A 两级选择的交互。
 */

beforeAll(() => {
  const rect = () => ({ left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0, toJSON() {} }) as DOMRect;
  const rects = () => Object.assign([], { item: () => null }) as unknown as DOMRectList;
  Range.prototype.getBoundingClientRect = rect;
  Range.prototype.getClientRects = rects;
  Element.prototype.getClientRects = rects;
});

const apply = (s: string, cs: { from: number; to: number; insert: string }[]) => {
  let out = s;
  for (const c of [...cs].sort((a, b) => b.from - a.from)) out = out.slice(0, c.from) + c.insert + out.slice(c.to);
  return out;
};

describe('纯函数', () => {
  const src = '| 左 | 中 | 右 |\n|:--|:-:|--:|\n| a \\| b | **2** | 3 |\n| x |  | z |';
  const model = parseTable(src, 0);

  it('子表：所选第一行作表头，分隔行带原表对齐，单元格取源码原文', () => {
    expect(subTableMarkdown(src, model, { row: 1, col: 0 }, { row: 2, col: 1 })).toBe('| a \\| b | **2** |\n| :-- | :-: |\n| x |  |');
    expect(subTableMarkdown(src, model, { row: 2, col: 2 }, { row: 0, col: 1 })).toBe('| 中 | 右 |\n| :-: | --: |\n| **2** | 3 |\n|  | z |');
  });

  it('清空所选单元格只删各格内容', () => {
    const cs = clearCells(src, model, { row: 1, col: 1 }, { row: 2, col: 2 }, 0);
    expect(apply(src, cs)).toBe('| 左 | 中 | 右 |\n|:--|:-:|--:|\n| a \\| b |  |  |\n| x |  |  |');
    expect(clearCells(src, model, { row: 2, col: 1 }, { row: 2, col: 1 }, 0)).toEqual([]);
  });

  const del = (doc: string) => {
    const t = Text.of(doc.split('\n'));
    const from = doc.indexOf('| t |'), to = from + '| t |\n|---|'.length;
    const { change, cursor } = tableDeletion(t, from, to);
    return { out: apply(doc, [change]), cursor };
  };
  it('删除整表：前后都是空行时只留一个空行', () => {
    expect(del('p\n\n| t |\n|---|\n\nq\n')).toEqual({ out: 'p\n\nq\n', cursor: 3 });
  });
  it('删除整表：在文首、文末、紧挨着别的块', () => {
    expect(del('| t |\n|---|\n\nq').out).toBe('q');
    expect(del('p\n\n| t |\n|---|').out).toBe('p\n');
    expect(del('p\n\n| t |\n|---|\n').out).toBe('p\n\n');
    expect(del('| t |\n|---|').out).toBe('');
    expect(del('# h\n| t |\n|---|\n# g').out).toBe('# h\n# g');
  });
});

const editors: TypedownEditor[] = [];
afterEach(() => {
  for (const ed of editors.splice(0)) { cellSession(ed.view)?.close(Leave.Stay); ed.view.destroy(); ed.view.dom.parentElement?.remove(); }
});

function mount(doc: string, cursor = 0): TypedownEditor {
  const parent = document.createElement('div');
  document.body.appendChild(parent);
  const ed = createEditor({ doc, parent });
  ensureSyntaxTree(ed.view.state, doc.length, 1e9);
  ed.view.dispatch({ selection: { anchor: cursor } });
  editors.push(ed);
  return ed;
}
const wrapOf = (view: EditorView) => view.contentDOM.querySelector<TableDom>('.cm-td-table-wrap')!;
const press = (view: EditorView, key: string, mod = false) =>
  runScopeHandlers(view, new KeyboardEvent('keydown', { key, ctrlKey: mod, bubbles: true, cancelable: true }), 'editor');

describe('整表选中', () => {
  const doc = '前一段\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n后一段\n';
  const tFrom = doc.indexOf('| a'), tTo = doc.indexOf('| 2 |') + 5;

  // jsdom 没有布局，上下方向键的移动（moveVertically）量不出来：这里用左右方向键移入，上下键移入在无头浏览器里手测
  it('方向键从表格外移入先选中整表，再按一次进入首格', () => {
    const ed = mount(doc, doc.indexOf('\n\n| a') + 1);
    expect(press(ed.view, 'ArrowRight')).toBe(true);
    const ts = ed.view.state.field(tableSelectionField)!;
    expect(ts).toMatchObject({ from: tFrom, to: tTo, whole: true });
    expect(ed.view.state.selection.main).toMatchObject({ from: tFrom, to: tTo });
    expect(wrapOf(ed.view).classList.contains('cm-td-table-selected')).toBe(true);
    expect(wrapOf(ed.view).querySelectorAll('.cm-td-cell-selected')).toHaveLength(4);
    expect(selectionText(ed.view.state, ts)).toBe(doc.slice(tFrom, tTo));
    press(ed.view, 'ArrowDown');
    const s = cellSession(ed.view)!;
    expect([s.row, s.col]).toEqual([0, 0]);
    expect(ed.view.state.field(tableSelectionField)).toBeNull();
    expect(wrapOf(ed.view).querySelectorAll('.cm-td-cell-selected')).toHaveLength(0);
  });

  it('从下方移入：再按向上进入末行', () => {
    const ed = mount(doc, doc.indexOf('\n\n后') + 1);
    press(ed.view, 'ArrowLeft');
    expect(ed.view.state.field(tableSelectionField)?.whole).toBe(true);
    press(ed.view, 'ArrowUp');
    expect([cellSession(ed.view)!.row, cellSession(ed.view)!.col]).toEqual([1, 0]);
  });

  it('单元格内 Ctrl+A 先选格内内容，再按选中整表；Backspace 删除整表，可撤销', () => {
    const ed = mount(doc);
    const s = openCell(ed.view, wrapOf(ed.view), 1, 1, 0)!;
    expect(press(s.nested, 'a', true)).toBe(true);
    expect(s.nested.state.selection.main).toMatchObject({ from: 0, to: 1 });
    expect(ed.view.state.field(tableSelectionField)).toBeNull();
    press(s.nested, 'a', true);
    expect(s.closed).toBe(true);
    expect(ed.view.state.field(tableSelectionField)?.whole).toBe(true);
    press(ed.view, 'Backspace');
    expect(ed.getText()).toBe('前一段\n\n后一段\n');
    undo(ed.view);
    expect(ed.getText()).toBe(doc);
  });

  it('选中时键入不替换表格，Escape 取消并把光标放到表格末尾', () => {
    const ed = mount(doc, doc.indexOf('\n\n| a') + 1);
    press(ed.view, 'ArrowRight');
    ed.view.contentDOM.dispatchEvent(new InputEvent('beforeinput', { inputType: 'insertText', data: 'x', bubbles: true, cancelable: true }));
    expect(ed.getText()).toBe(doc);
    press(ed.view, 'Escape');
    expect(ed.view.state.field(tableSelectionField)).toBeNull();
    expect(ed.view.state.selection.main.head).toBe(tTo);
  });
});

describe('矩形多选', () => {
  const doc = '| a | b | c |\n|:-:|---|---|\n| 1 | 2 | 3 |\n| 4 | 5 | 6 |\n';

  it('拖到另一格时按矩形选中，复制得到子表，Delete 只清空这些格', () => {
    const ed = mount(doc);
    const wrap = wrapOf(ed.view);
    const cells = wrap.tdCells!;
    openCell(ed.view, wrap, 1, 0, 0);
    // jsdom 没有布局：elementFromPoint 按预设的目标返回
    let over: Element = cells[2][1];
    document.elementFromPoint = () => over;
    trackCellDrag(ed.view, wrap, { row: 1, col: 0 }, 0);
    document.dispatchEvent(new MouseEvent('mousemove', { buttons: 1, clientX: 1, clientY: 1 }));
    expect(cellSession(ed.view)).toBeNull();
    const ts = ed.view.state.field(tableSelectionField)!;
    expect(ts).toMatchObject({ whole: false, anchor: { row: 1, col: 0 }, head: { row: 2, col: 1 } });
    expect(wrap.querySelectorAll('.cm-td-cell-selected')).toHaveLength(4);
    expect(cells[1][0].parentElement!.className).toContain('cm-td-cell-border-top');
    expect(cells[2][1].parentElement!.className).toContain('cm-td-cell-border-right');
    expect(selectionText(ed.view.state, ts)).toBe('| 1 | 2 |\n| :-: | --- |\n| 4 | 5 |');
    over = cells[0][2];
    document.dispatchEvent(new MouseEvent('mousemove', { buttons: 1 }));
    document.dispatchEvent(new MouseEvent('mouseup'));
    expect(ed.view.state.field(tableSelectionField)).toMatchObject({ head: { row: 0, col: 2 } });
    press(ed.view, 'Delete');
    expect(ed.getText()).toBe('|  |  |  |\n|:-:|---|---|\n|  |  |  |\n| 4 | 5 | 6 |\n');
    // 清空后块选择保持，再按方向键进入 head 所在格
    expect(ed.view.state.field(tableSelectionField)).not.toBeNull();
    press(ed.view, 'ArrowRight');
    expect([cellSession(ed.view)!.row, cellSession(ed.view)!.col]).toEqual([0, 2]);
  });
});
