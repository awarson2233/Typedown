/**
 * GFM 表格的「单元格 ↔ 源码区间」映射（webview-wysiwyg-engine.md 第 6 节）。纯函数：
 * 输入表格源码与它在文档中的起点，输出每个单元格内容在文档中的区间；单元格编辑只替换该区间，
 * 其余单元格与对齐空格一个字节都不动。
 */

export type Align = 'left' | 'center' | 'right' | null;

export interface CellRange {
  /** 单元格内容（去掉两侧空白）在文档中的区间；空单元格是一个零长度的插入点 */
  from: number;
  to: number;
}

export interface TableRow {
  from: number;
  to: number;
  cells: CellRange[];
}

export interface TableModel {
  header: TableRow;
  delimiter: TableRow;
  body: TableRow[];
  aligns: Align[];
  cols: number;
}

const isSpace = (c: string) => c === ' ' || c === '\t';

/** 拆一行：返回每个单元格的内容区间（相对行首）。 */
export function splitRow(line: string): CellRange[] {
  let i = 0;
  while (i < line.length && isSpace(line[i])) i++;
  const leadingPipe = line[i] === '|';
  if (leadingPipe) i++;
  const segs: { from: number; to: number }[] = [];
  let segStart = i;
  for (; i < line.length; i++) {
    const c = line[i];
    if (c === '\\') { i++; continue; }
    if (c === '|') { segs.push({ from: segStart, to: i }); segStart = i + 1; }
  }
  const tail = { from: segStart, to: line.length };
  const tailBlank = line.slice(tail.from, tail.to).trim() === '';
  // 末尾竖线之后的空白不是单元格；没有末尾竖线时最后一段是单元格
  if (!(tailBlank && segs.length > 0)) segs.push(tail);
  return segs.map(s => {
    let a = s.from, b = s.to;
    while (a < b && isSpace(line[a])) a++;
    while (b > a && isSpace(line[b - 1])) b--;
    if (a === b) {
      // 空单元格：插入点放在左竖线后的第一个空格之后，打字得到 `| x |` 而不是 `|x  |`
      const p = s.from < s.to && isSpace(line[s.from]) ? s.from + 1 : s.from;
      return { from: p, to: p };
    }
    return { from: a, to: b };
  });
}

function alignOf(cell: string): Align {
  const t = cell.trim();
  const l = t.startsWith(':'), r = t.endsWith(':');
  return l && r ? 'center' : r ? 'right' : l ? 'left' : null;
}

/** 解析表格源码（多行，行间 `\n`）；offset 为表格在文档中的起点。 */
export function parseTable(text: string, offset: number): TableModel {
  const rows: TableRow[] = [];
  let pos = 0;
  for (const line of text.split('\n')) {
    const cells = splitRow(line).map(c => ({ from: c.from + offset + pos, to: c.to + offset + pos }));
    rows.push({ from: offset + pos, to: offset + pos + line.length, cells });
    pos += line.length + 1;
  }
  const [header, delimiter, ...body] = rows;
  const aligns = delimiter.cells.map(c => alignOf(text.slice(c.from - offset, c.to - offset)));
  return { header, delimiter, body, aligns, cols: header.cells.length };
}

/**
 * 单元格显示文本 ↔ 源码：GFM 要求单元格里的竖线写成 `\|`。显示文本里已经是 `\|` 的保持原样。
 * 换行不能出现在单元格里，换成空格。不处理不换行空格：编辑面用 white-space: pre-wrap，浏览器不会插入 &nbsp;。
 */
export const unescapeCell = (src: string) => src.replace(/\\\|/g, '|');
export const escapeCell = (text: string) =>
  text.replace(/\r?\n/g, ' ').replace(/\\\|/g, '\u0000').replace(/\|/g, '\\|').replace(/\u0000/g, '\\|');

export interface SimpleChange { from: number; to: number; insert: string }

/**
 * 单元格源码里不能出现的字符的逐处修正：未转义的竖线前补 `\`（前面的反斜杠成对时竖线仍是未转义的，`\\|` 也要补），
 * 换行换成空格。返回的区间是相对 text 的，互不重叠、按位置排序，可以直接作为一组 changes 使用（光标随之映射）。
 */
export function cellSourceFixes(text: string): SimpleChange[] {
  const out: SimpleChange[] = [];
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (c === '\\') { if (text[i + 1] !== '\n' && text[i + 1] !== '\r') i++; continue; }
    if (c === '|') out.push({ from: i, to: i, insert: '\\' });
    else if (c === '\r' && text[i + 1] === '\n') { out.push({ from: i, to: i + 2, insert: ' ' }); i++; }
    else if (c === '\n' || c === '\r') out.push({ from: i, to: i + 1, insert: ' ' });
  }
  return out;
}

/** 按 cellSourceFixes 修正后的单元格源码 */
export function toCellSource(text: string): string {
  let out = '', last = 0;
  for (const f of cellSourceFixes(text)) { out += text.slice(last, f.from) + f.insert; last = f.to; }
  return out + text.slice(last);
}

/** 最小替换：只替换旧源码与新源码之间不同的中段。 */
export function minimalChange(oldText: string, newText: string, at: number): SimpleChange | null {
  if (oldText === newText) return null;
  let p = 0;
  const max = Math.min(oldText.length, newText.length);
  while (p < max && oldText.charCodeAt(p) === newText.charCodeAt(p)) p++;
  let s = 0;
  while (s < max - p && oldText.charCodeAt(oldText.length - 1 - s) === newText.charCodeAt(newText.length - 1 - s)) s++;
  return { from: at + p, to: at + oldText.length - s, insert: newText.slice(p, newText.length - s) };
}

export const tableRows = (m: TableModel): TableRow[] => [m.header, ...m.body];

/**
 * 单元格 (row, col) 的编辑：row 0 是表头，1… 是表体。`doc` 取区间文本用。
 * 行缺单元格（GFM 允许表体行比表头短）时在行尾补齐竖线再写入。
 */
export function cellEdit(sliceDoc: (from: number, to: number) => string, model: TableModel, row: number, col: number, text: string): SimpleChange | null {
  return cellReplace(sliceDoc, model, row, col, escapeCell(text));
}

/** 同 cellEdit，但 src 已经是单元格源码（竖线已转义、没有换行），原样写入。嵌套视图的单元格文档就是源码，走这里。 */
export function cellReplace(sliceDoc: (from: number, to: number) => string, model: TableModel, row: number, col: number, src: string): SimpleChange | null {
  const r = tableRows(model)[row];
  if (!r) return null;
  const cell = r.cells[col];
  if (cell) return minimalChange(sliceDoc(cell.from, cell.to), src, cell.from);
  // 缺的单元格：在行尾追加。行尾有竖线时写在它后面，没有时先补一个
  const line = sliceDoc(r.from, r.to);
  const trimmedEnd = line.replace(/[ \t]+$/, '');
  const endsWithPipe = trimmedEnd.endsWith('|') && !trimmedEnd.endsWith('\\|');
  let insert = endsWithPipe ? '' : ' |';
  for (let c = r.cells.length; c < col; c++) insert += '  |';
  insert += ` ${src} |`;
  const at = r.from + trimmedEnd.length;
  return { from: at, to: at, insert };
}

