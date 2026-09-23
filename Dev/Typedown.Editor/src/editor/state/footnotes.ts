import { MapMode, StateField, type ChangeDesc, type EditorState, type Text } from '@codemirror/state';
import { normalizeFootnoteLabel } from '../syntax/footnote';

/**
 * 脚注编号：按引用 `[^label]` 在全文中首次出现的顺序编 1、2、3…；没有被引用的定义排在后面，按定义出现的顺序接着编。
 * 编号要覆盖全文，而语法树只可信到 parsedLength，所以和大纲一样只扫源文本（逐行正则），按改动的行增量维护。
 * 与语法树口径的差别：代码块、行内代码里的 `[^x]` 也会被当作引用计数（只影响编号，不影响渲染）。
 * 编号表的键是 normalizeFootnoteLabel 规范化后的 label（[^Note] 与 [^note] 是同一个脚注）。
 * 行内引用的上标（W1，经 footnoteNumberSource 注入 footnoteNumbers）与脚注定义区（块组件）用同一张表。
 */

export interface FootnoteMention {
  readonly pos: number;
  readonly label: string;
  /** 行首的定义 `[^label]:`；否则是行内引用 */
  readonly definition: boolean;
}

export interface FootnoteIndex {
  /** 按位置排序 */
  readonly mentions: readonly FootnoteMention[];
  /** 规范化后的 label → 编号 */
  readonly numbers: ReadonlyMap<string, number>;
  /** 编号表变化时 +1（只是位置移动不变），依赖编号的 widget 据此刷新 */
  readonly revision: number;
}

const REF = /\[\^([^\s\]^[]+)\](:?)/g;
const DEF_AT_LINE_START = /^ {0,3}(?:> ?)*\[\^([^\s\]^[]+)\]:/;

function scanLine(text: string, lineFrom: number, out: FootnoteMention[]) {
  const def = DEF_AT_LINE_START.exec(text);
  const defAt = def ? def[0].length - def[1].length - 4 : -1; // `[` 的列
  REF.lastIndex = 0;
  for (let m: RegExpExecArray | null; (m = REF.exec(text)); ) {
    if (m.index === defAt) out.push({ pos: lineFrom + m.index, label: m[1], definition: true });
    else if (!m[2]) out.push({ pos: lineFrom + m.index, label: m[1], definition: false });
  }
}

function scanRange(doc: Text, from: number, to: number): FootnoteMention[] {
  const out: FootnoteMention[] = [];
  if (from > to) return out;
  for (let l = doc.lineAt(from); ; l = doc.line(l.number + 1)) {
    // 行里没有 `[^` 时跳过正则（绝大多数行）
    if (l.text.includes('[^')) scanLine(l.text, l.from, out);
    if (l.to >= to || l.number >= doc.lines) break;
  }
  return out;
}

function numbering(mentions: readonly FootnoteMention[]): Map<string, number> {
  const numbers = new Map<string, number>();
  for (const def of [false, true]) {
    for (const m of mentions) {
      if (m.definition !== def) continue;
      const key = normalizeFootnoteLabel(m.label);
      if (!numbers.has(key)) numbers.set(key, numbers.size + 1);
    }
  }
  return numbers;
}

const sameNumbers = (a: ReadonlyMap<string, number>, b: ReadonlyMap<string, number>) => {
  if (a.size !== b.size) return false;
  for (const [k, v] of a) if (b.get(k) !== v) return false;
  return true;
};

export function scanFootnotes(doc: Text): FootnoteIndex {
  const mentions = scanRange(doc, 0, doc.length);
  return { mentions, numbers: numbering(mentions), revision: 0 };
}

export function updateFootnotes(value: FootnoteIndex, newDoc: Text, changes: ChangeDesc): FootnoteIndex {
  if (changes.empty) return value;
  // 改动所在的整行（新文档坐标）重扫，其余条目按变化映射
  const dirty: [number, number][] = [];
  changes.iterChangedRanges((_fA, _tA, fB, tB) => dirty.push([newDoc.lineAt(fB).from, newDoc.lineAt(tB).to]));
  const inDirty = (p: number) => dirty.some(([f, t]) => p >= f && p <= t);
  const kept: FootnoteMention[] = [];
  for (const m of value.mentions) {
    // 被删掉的条目（映射后落在改动行里或原位置被删除）一律丢弃，交给重扫
    const pos = changes.mapPos(m.pos, 1, MapMode.TrackDel);
    if (pos === null || inDirty(pos)) continue;
    kept.push(pos === m.pos ? m : { ...m, pos });
  }
  for (const [f, t] of dirty) kept.push(...scanRange(newDoc, f, t));
  kept.sort((a, b) => a.pos - b.pos);
  const mentions = kept.filter((m, i) => i === 0 || m.pos !== kept[i - 1].pos);
  const numbers = numbering(mentions);
  return { mentions, numbers: sameNumbers(numbers, value.numbers) ? value.numbers : numbers, revision: sameNumbers(numbers, value.numbers) ? value.revision : value.revision + 1 };
}

export const footnoteField = StateField.define<FootnoteIndex>({
  create: state => scanFootnotes(state.doc),
  update: (value, tr) => (tr.docChanged ? updateFootnotes(value, tr.state.doc, tr.changes) : value),
});

const NO_NUMBERS: ReadonlyMap<string, number> = new Map();

/** 全文的编号表（键是规范化后的 label）；状态里没装 footnoteField 时为空表 */
export function footnoteNumbers(state: EditorState): ReadonlyMap<string, number> {
  return state.field(footnoteField, false)?.numbers ?? NO_NUMBERS;
}

/** label（源码原文即可，内部规范化）的编号；没有这个脚注时为 0 */
export function footnoteNumber(state: EditorState, label: string): number {
  return footnoteNumbers(state).get(normalizeFootnoteLabel(label)) ?? 0;
}

/** 第一处引用 `[^label]` 的位置（定义区的返回链接用），没有时为 -1 */
export function firstReference(state: EditorState, label: string): number {
  const idx = state.field(footnoteField, false);
  const key = normalizeFootnoteLabel(label);
  const m = idx?.mentions.find(x => !x.definition && normalizeFootnoteLabel(x.label) === key);
  return m ? m.pos : -1;
}
