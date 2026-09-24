import type { Text } from '@codemirror/state';
import { tableRows, type Align, type SimpleChange, type TableModel } from './tableCells';

/**
 * 表格块选择的纯函数（webview-wysiwyg-engine.md 第 6 节「选择」）：矩形规范化、复制用的 markdown 子表、清空所选单元格、删除整表。
 * 都是「源文本 + 表格模型 → 字符串或最小源码替换」，视图一侧见 widgets/tableSelection.ts。
 */

/** 单元格坐标：row 0 是表头，1… 是表体 */
export interface CellPos { readonly row: number; readonly col: number }

/** 矩形选区（两个角，顺序不限）规范成左上、右下 */
export function normalizeRect(a: CellPos, b: CellPos): { top: number; left: number; bottom: number; right: number } {
  return { top: Math.min(a.row, b.row), left: Math.min(a.col, b.col), bottom: Math.max(a.row, b.row), right: Math.max(a.col, b.col) };
}

const delimiterCell = (a: Align) => (a === 'center' ? ':-:' : a === 'left' ? ':--' : a === 'right' ? '--:' : '---');

/**
 * 所选矩形的 markdown 子表：单元格取源码原文（`\|` 等转义保留），所选的第一行作表头，分隔行带原表的对齐。
 * model 的区间以 src 起点为 0。
 */
export function subTableMarkdown(src: string, model: TableModel, a: CellPos, b: CellPos): string {
  const { top, left, bottom, right } = normalizeRect(a, b);
  const rows = tableRows(model);
  const line = (cells: string[]) => `| ${cells.join(' | ')} |`;
  const out: string[] = [];
  for (let r = top; r <= bottom && r < rows.length; r++) {
    const cells: string[] = [];
    for (let c = left; c <= right && c < model.cols; c++) {
      const cell = rows[r].cells[c];
      cells.push(cell ? src.slice(cell.from, cell.to) : '');
    }
    out.push(line(cells));
    if (r === top) out.push(line(cells.map((_, i) => delimiterCell(model.aligns[left + i] ?? null))));
  }
  return out.join('\n');
}

/** 清空所选矩形里的单元格：只删各格内容（两侧空白与竖线不动），区间按 offset 平移成文档位置，按位置排序 */
export function clearCells(src: string, model: TableModel, a: CellPos, b: CellPos, offset: number): SimpleChange[] {
  const { top, left, bottom, right } = normalizeRect(a, b);
  const rows = tableRows(model);
  const out: SimpleChange[] = [];
  for (let r = top; r <= bottom && r < rows.length; r++) {
    for (let c = left; c <= right; c++) {
      const cell = rows[r].cells[c];
      if (cell && cell.to > cell.from && src.slice(cell.from, cell.to)) out.push({ from: offset + cell.from, to: offset + cell.to, insert: '' });
    }
  }
  return out;
}

/**
 * 删除整张表格（from..to 是表格首行行首到末行行尾）：连同表格后的换行一起删；后面是空行、前面是空行或文首时
 * 再删掉后面那个空行，块与块之间仍只隔一个空行（文首不留空行）。表格在文末时删掉它前面的换行。返回替换与删除后光标的位置（原表格所在处）。
 */
export function tableDeletion(doc: Text, from: number, to: number): { change: SimpleChange; cursor: number } {
  const first = doc.lineAt(from), last = doc.lineAt(to);
  const blank = (n: number) => n >= 1 && n <= doc.lines && doc.line(n).text.trim() === '';
  if (last.number < doc.lines) {
    let end = last.to + 1;
    const next = doc.line(last.number + 1);
    if (blank(next.number) && (first.number === 1 || blank(first.number - 1))) end = next.number < doc.lines ? next.to + 1 : next.to;
    return { change: { from: first.from, to: end, insert: '' }, cursor: first.from };
  }
  if (first.number > 1) return { change: { from: first.from - 1, to: last.to, insert: '' }, cursor: first.from - 1 };
  return { change: { from: first.from, to: last.to, insert: '' }, cursor: first.from };
}
