import { StateField, type EditorState, type Range, type Transaction } from '@codemirror/state';
import { Decoration, EditorView, type DecorationSet } from '@codemirror/view';
import { syntaxTree } from '@codemirror/language';
import type { Tree } from '@lezer/common';
import { iterateTopBlocks } from '../syntax';
import { parsedLength } from '../state/parseProgress';
import { hasRefresh, isComposeTransaction, revealFrozen } from '../decorations/revealState';
import { revealedBy } from '../decorations/inlineSpecs';
import { DiagramWidget } from './blockWidgets';
import { TableWidget } from './tableWidget';

/**
 * 块组件 StateField（webview-wysiwyg-engine.md 第 4 节；wysiwyg-engine-survey.md 第 6.8 节）。
 * 块级 widget 会影响纵向布局，只能由 StateField 直接提供；StateField 没有视口概念，所以：
 * - 按变化区间增量维护：只重扫「变化区间 + 新旧语法树里覆盖它的顶层块 + 新旧显形位置所在的块」，其余按变化映射；
 * - 语法树只覆盖到 parsedLength 为止，后台解析推进（Language.setState 事务）时补扫新覆盖的区段；
 * - 组字事务只映射；鼠标冻结期间不随选区切换两态。
 */

export interface BlockSpec {
  kind: 'table' | 'math' | 'mermaid';
  from: number;
  to: number;
  src: string;
  editing: boolean;
}

export interface BlockState {
  decos: DecorationSet;
  /** 已按语法树扫描过的范围终点（之后的块等解析推进再扫） */
  covered: number;
  /** 用于两态判断的位置（选区端点；冻结与组字期间只映射） */
  reveal: number[];
}

const selectionPoints = (state: EditorState) => {
  const pts: number[] = [];
  for (const r of state.selection.ranges) { pts.push(r.head); if (r.anchor !== r.head) pts.push(r.anchor); }
  return pts;
};

/** 语法树可信的终点：未解析完时，最后一个顶层块可能被截断，退到它的起点。 */
export function coveredEnd(state: EditorState, tree: Tree): number {
  const len = parsedLength(state);
  if (len >= state.doc.length) return state.doc.length;
  let last = len;
  iterateTopBlocks(tree, Math.max(0, len - 1), len, n => { last = Math.min(last, state.doc.lineAt(n.from).from); });
  return last;
}

/** 扫描 [from, to] 内、且终点不超过 limit 的顶层块，产出块组件描述。纯函数。 */
export function scanBlocks(state: EditorState, tree: Tree, from: number, to: number, limit: number, reveal: readonly number[]): BlockSpec[] {
  const out: BlockSpec[] = [];
  const doc = state.doc;
  iterateTopBlocks(tree, from, to, n => {
    if (n.to > limit) return;
    const name = n.name;
    if (name !== 'Table' && name !== 'BlockMath' && name !== 'FencedCode') return;
    const first = doc.lineAt(n.from), last = doc.lineAt(n.to);
    if (name === 'Table') {
      out.push({ kind: 'table', from: first.from, to: last.to, src: doc.sliceString(first.from, last.to), editing: false });
      return;
    }
    const node = n.node;
    let kind: 'math' | 'mermaid';
    if (name === 'BlockMath') {
      const marks = node.getChildren('BlockMathMark');
      if (marks.length < 2) return; // 未闭合的公式块按源码显示
      kind = 'math';
    } else {
      const info = node.getChild('CodeInfo');
      if (!info || !/^mermaid$/i.test(doc.sliceString(info.from, info.to).trim())) return;
      if (node.getChildren('CodeMark').length < 2) return;
      kind = 'mermaid';
    }
    if (last.number - first.number < 1) return;
    const src = last.number - first.number >= 2 ? doc.sliceString(doc.line(first.number + 1).from, doc.line(last.number - 1).to) : '';
    out.push({ kind, from: first.from, to: last.to, src, editing: revealedBy(reveal, first.from, last.to) });
  });
  return out;
}

export function specToDecoration(s: BlockSpec): Range<Decoration> {
  if (s.kind === 'table') return Decoration.replace({ widget: new TableWidget(s.src), block: true }).range(s.from, s.to);
  if (s.editing) return Decoration.widget({ widget: new DiagramWidget(s.kind, s.src, true), block: true, side: 1 }).range(s.to);
  return Decoration.replace({ widget: new DiagramWidget(s.kind, s.src, false), block: true }).range(s.from, s.to);
}

/** 把区间扩到覆盖它的顶层块边界（按行对齐）。 */
function expandToBlocks(state: EditorState, tree: Tree, from: number, to: number): [number, number] {
  let f = from, t = to;
  iterateTopBlocks(tree, from, to, n => {
    f = Math.min(f, state.doc.lineAt(n.from).from);
    t = Math.max(t, state.doc.lineAt(n.to).to);
  });
  return [f, t];
}

function mergeRanges(rs: [number, number][]): [number, number][] {
  rs.sort((a, b) => a[0] - b[0]);
  const out: [number, number][] = [];
  for (const r of rs) {
    const last = out[out.length - 1];
    if (last && r[0] <= last[1] + 1) last[1] = Math.max(last[1], r[1]);
    else out.push([r[0], r[1]]);
  }
  return out;
}

const sameReveal = (a: readonly number[], b: readonly number[]) => a.length === b.length && a.every((p, i) => p === b[i]);

export function createBlockState(state: EditorState): BlockState {
  const tree = syntaxTree(state);
  const covered = coveredEnd(state, tree);
  const reveal = selectionPoints(state);
  const specs = scanBlocks(state, tree, 0, covered, covered, reveal);
  return { decos: Decoration.set(specs.map(specToDecoration), true), covered, reveal };
}

/** 统计每次更新重扫的字符数，给性能探针与单测用 */
export const blockFieldStats = { updates: 0, rescannedChars: 0, lastRescan: 0 };

export function updateBlockState(value: BlockState, tr: Transaction): BlockState {
  if (isComposeTransaction(tr)) {
    if (!tr.docChanged) return value;
    return {
      decos: value.decos.map(tr.changes),
      covered: tr.changes.mapPos(value.covered, -1),
      reveal: value.reveal.map(p => tr.changes.mapPos(p)),
    };
  }
  const state = tr.state;
  const tree = syntaxTree(state);
  const oldTree = syntaxTree(tr.startState);
  const frozen = state.field(revealFrozen, false) ?? false;
  const refresh = hasRefresh(tr);
  const mappedReveal = tr.docChanged ? value.reveal.map(p => tr.changes.mapPos(p)) : value.reveal;
  const reveal = !frozen && (tr.selection || refresh || tr.docChanged) ? selectionPoints(state) : mappedReveal;
  const newCovered = tree === oldTree && !tr.docChanged ? value.covered : coveredEnd(state, tree);
  const mappedCovered = tr.docChanged ? tr.changes.mapPos(value.covered, -1) : value.covered;
  const revealChanged = !sameReveal(reveal, mappedReveal) || refresh;
  if (!tr.docChanged && !revealChanged && newCovered <= value.covered) return value;

  const dirty: [number, number][] = [];
  if (tr.docChanged) {
    tr.changes.iterChangedRanges((fromA, toA, fromB, toB) => {
      dirty.push([fromB, toB]);
      iterateTopBlocks(oldTree, fromA, toA, n => {
        dirty.push([tr.changes.mapPos(n.from, -1), tr.changes.mapPos(n.to, 1)]);
      });
    });
  }
  if (revealChanged) for (const p of [...mappedReveal, ...reveal]) dirty.push([p, p]);
  if (newCovered > mappedCovered) dirty.push([mappedCovered, newCovered]);

  let decos = tr.docChanged ? value.decos.map(tr.changes) : value.decos;
  const add: Range<Decoration>[] = [];
  let rescanned = 0;
  // 事务内的同步解析有时间上限（@codemirror/language 的 Work.Apply），编辑后语法树可能比上次短。
  // 旧覆盖范围内、新语法树之后的装饰是按变化映射过来的旧结果，只要不落在脏区间里就仍然正确，
  // 所以覆盖终点保留到第一个越过新语法树的脏区间为止，免得后台解析追回来时整段重扫。
  let keep = mappedCovered;
  for (const [a, b] of mergeRanges(dirty.map(([a, b]) => expandToBlocks(state, tree, a, b)))) {
    const [f, t] = expandToBlocks(state, tree, a, b);
    decos = decos.update({ filterFrom: f, filterTo: t, filter: (x, y) => y < f || x > t });
    if (f <= newCovered) {
      for (const s of scanBlocks(state, tree, f, t, newCovered, reveal)) add.push(specToDecoration(s));
      rescanned += Math.min(t, newCovered) - f;
    }
    if (t > newCovered) keep = Math.min(keep, f);
  }
  if (add.length) decos = decos.update({ add, sort: true });
  blockFieldStats.updates++;
  blockFieldStats.lastRescan = rescanned;
  blockFieldStats.rescannedChars += rescanned;
  return { decos, covered: Math.max(newCovered, keep), reveal };
}

export const blockField = StateField.define<BlockState>({
  create: createBlockState,
  update: updateBlockState,
  provide: f => EditorView.decorations.from(f, v => v.decos),
});
