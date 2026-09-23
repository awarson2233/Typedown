import { RangeSet, RangeValue, StateField, type EditorState, type Range, type Transaction } from '@codemirror/state';
import { Decoration, EditorView, type DecorationSet } from '@codemirror/view';
import { syntaxTree } from '@codemirror/language';
import type { SyntaxNode, Tree } from '@lezer/common';
import { iterateTopBlocks } from '../syntax';
import { parsedLength } from '../state/parseProgress';
import { hasRefresh, isComposeTransaction, revealFrozen } from '../decorations/revealState';
import { revealedBy } from '../decorations/inlineSpecs';
import { outlineField, headingPlainText, type OutlineItem } from '../state/outline';
import { footnoteField, footnoteNumber } from '../state/footnotes';
import {
  DiagramWidget, FenceRole, FenceWidget, FencePrefixWidget, FootnoteBackWidget, FootnoteLabelWidget, HtmlWidget,
  LanguagePlaceholderWidget, TocWidget, isInvisibleHtml, type FenceKind, type TocEntry,
} from './blockWidgets';
import { TableWidget } from './tableWidget';
import { codeLanguageId } from '../blocks/codeLanguage';

/**
 * 块组件 StateField（webview-wysiwyg-engine.md 第 4 节；wysiwyg-engine-survey.md 第 6.8 节）。
 * 块级 widget 会影响纵向布局，只能由 StateField 直接提供；StateField 没有视口概念，所以：
 * - 按变化区间增量维护：只重扫「变化区间 + 新旧语法树里覆盖它的顶层块 + 新旧显形位置所在的块」，其余按变化映射；
 * - 语法树只覆盖到 parsedLength 为止，后台解析推进（Language.setState 事务）时补扫新覆盖的区段；
 * - 依赖全文索引的块（`[TOC]` 依赖大纲、脚注定义依赖编号）另记在 globals 里，索引的 revision 变化时一并重扫；
 * - 组字事务只映射；鼠标冻结期间不随选区切换两态。
 *
 * 只处理顶层块；列表、引用里的代码块、公式块等仍按源码显示。
 */

/** 围栏式块的几何（都是文档偏移） */
export interface FenceGeometry {
  /** 开围栏行 */
  readonly openFrom: number;
  readonly openTo: number;
  /** 闭围栏行；未闭合（代码块延续到文末）时为 null */
  readonly closeFrom: number | null;
  readonly closeTo: number | null;
  /** 内容行的范围（第一行行首到最后一行行尾）；没有内容行时为 null */
  readonly bodyFrom: number | null;
  readonly bodyTo: number | null;
}

interface SpecBase { readonly from: number; readonly to: number }

export type BlockSpec =
  | SpecBase & { readonly kind: 'table'; readonly src: string }
  | SpecBase & { readonly kind: 'math' | 'mermaid'; readonly src: string; readonly editing: boolean; readonly fence: FenceGeometry; readonly info: InfoRange | null }
  | SpecBase & { readonly kind: 'code'; readonly lang: string; readonly active: boolean; readonly fence: FenceGeometry; readonly info: InfoRange | null }
  | SpecBase & { readonly kind: 'frontmatter'; readonly active: boolean; readonly fence: FenceGeometry }
  | SpecBase & { readonly kind: 'html'; readonly src: string; readonly editing: boolean }
  | SpecBase & { readonly kind: 'toc'; readonly entries: readonly TocEntry[]; readonly editing: boolean }
  | SpecBase & { readonly kind: 'footnote'; readonly label: string; readonly number: number; readonly markTo: number; readonly revealed: boolean };

/** 代码块首行里信息串（语言名）的范围 */
export interface InfoRange { readonly from: number; readonly to: number }

export type BlockKind = BlockSpec['kind'];

export interface BlockState {
  decos: DecorationSet;
  /** 每个块组件覆盖的范围：重扫时脏区间要扩到覆盖所有与它相交的旧块，否则结构变化后旧块伸出去的部分会残留 */
  extents: RangeSet<GlobalBlock>;
  /** 依赖全文索引的块（toc、footnote）的范围，索引变化时据此重扫 */
  globals: RangeSet<GlobalBlock>;
  /** 已按语法树扫描过的范围终点（之后的块等解析推进再扫） */
  covered: number;
  /** 用于两态判断的位置（选区端点；冻结与组字期间只映射） */
  reveal: number[];
  /** 生成这些装饰时的大纲与脚注编号版本 */
  outlineRev: number;
  footnoteRev: number;
}

export class GlobalBlock extends RangeValue {
  constructor(readonly kind: BlockKind) { super(); }
  eq(o: GlobalBlock) { return o.kind === this.kind; }
}
const EXTENT = new Map<BlockKind, GlobalBlock>();
const extentOf = (s: BlockSpec): Range<GlobalBlock> => {
  let v = EXTENT.get(s.kind);
  if (!v) EXTENT.set(s.kind, (v = new GlobalBlock(s.kind)));
  return v.range(s.from, s.to);
};

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

/** 围栏式块的几何：首行是开围栏；closed 时末行是闭围栏 */
function fenceGeometry(state: EditorState, from: number, to: number, closed: boolean): FenceGeometry {
  const doc = state.doc;
  const first = doc.lineAt(from), last = doc.lineAt(to);
  const bodyFirst = first.number + 1, bodyLast = closed ? last.number - 1 : last.number;
  const hasBody = bodyFirst <= bodyLast;
  return {
    openFrom: first.from, openTo: first.to,
    closeFrom: closed ? last.from : null, closeTo: closed ? last.to : null,
    bodyFrom: hasBody ? doc.line(bodyFirst).from : null,
    bodyTo: hasBody ? doc.line(bodyLast).to : null,
  };
}

const tocCache = new WeakMap<readonly OutlineItem[], TocEntry[]>();
function tocEntries(state: EditorState): readonly TocEntry[] {
  const headings = state.field(outlineField, false)?.headings;
  if (!headings) return [];
  let e = tocCache.get(headings);
  if (!e) tocCache.set(headings, (e = headings.map(h => ({ level: h.level, text: headingPlainText(h.text) }))));
  return e;
}

function codeInfo(node: SyntaxNode): InfoRange | null {
  const info = node.getChild('CodeInfo');
  return info ? { from: info.from, to: info.to } : null;
}

/** 扫描 [from, to] 内、且终点不超过 limit 的顶层块，产出块组件描述。纯函数。 */
export function scanBlocks(state: EditorState, tree: Tree, from: number, to: number, limit: number, reveal: readonly number[]): BlockSpec[] {
  const out: BlockSpec[] = [];
  const doc = state.doc;
  iterateTopBlocks(tree, from, to, n => {
    if (n.to > limit) return;
    // HTML 块的节点终点带着结尾换行（落在下一行行首），块只算到上一行
    const first = doc.lineAt(n.from), last = doc.lineAt(n.to > n.from && doc.lineAt(n.to).from === n.to ? n.to - 1 : n.to);
    const bFrom = first.from, bTo = last.to;
    const inside = revealedBy(reveal, bFrom, bTo);
    switch (n.name) {
      case 'Table':
        out.push({ kind: 'table', from: bFrom, to: bTo, src: doc.sliceString(bFrom, bTo) });
        return;
      case 'BlockMath': {
        if (n.node.getChildren('BlockMathMark').length < 2 || last.number - first.number < 1) return; // 未闭合的公式块按源码显示
        const fence = fenceGeometry(state, bFrom, bTo, true);
        const src = fence.bodyFrom === null ? '' : doc.sliceString(fence.bodyFrom, fence.bodyTo!);
        out.push({ kind: 'math', from: bFrom, to: bTo, src, editing: inside, fence, info: null });
        return;
      }
      case 'FencedCode': {
        const node = n.node;
        const info = codeInfo(node);
        const lang = info ? doc.sliceString(info.from, info.to) : '';
        const closed = node.getChildren('CodeMark').length >= 2 && last.number > first.number;
        const fence = fenceGeometry(state, bFrom, bTo, closed);
        if (/^mermaid$/i.test(lang.trim())) {
          if (!closed) return;
          const src = fence.bodyFrom === null ? '' : doc.sliceString(fence.bodyFrom, fence.bodyTo!);
          out.push({ kind: 'mermaid', from: bFrom, to: bTo, src, editing: inside, fence, info });
          return;
        }
        if (fence.bodyFrom === null) return; // 没有内容行：收起围栏后无处落光标，按源码显示
        out.push({ kind: 'code', from: bFrom, to: bTo, lang, active: inside, fence, info });
        return;
      }
      case 'Frontmatter': {
        const fence = fenceGeometry(state, bFrom, bTo, true);
        if (fence.bodyFrom === null) return;
        out.push({ kind: 'frontmatter', from: bFrom, to: bTo, active: inside, fence });
        return;
      }
      case 'CommentBlock': // 整块的 HTML 注释，按不可见的 HTML 块显示源码框
      case 'HTMLBlock': {
        const src = doc.sliceString(bFrom, bTo);
        // 注释、script、style 之类渲染出来是空白，始终按编辑态显示源码框
        out.push({ kind: 'html', from: bFrom, to: bTo, src, editing: inside || isInvisibleHtml(src) });
        return;
      }
      case 'TableOfContents':
        out.push({ kind: 'toc', from: bFrom, to: bTo, entries: tocEntries(state), editing: inside });
        return;
      case 'FootnoteDefinition': {
        const node = n.node;
        const label = node.getChild('FootnoteLabel');
        const marks = node.getChildren('FootnoteDefinitionMark');
        if (!label || marks.length < 2) return;
        const name = doc.sliceString(label.from, label.to);
        let markTo = marks[1].to;
        if (markTo < first.to && /[ \t]/.test(doc.sliceString(markTo, markTo + 1))) markTo++;
        const number = footnoteNumber(state, name);
        out.push({ kind: 'footnote', from: bFrom, to: bTo, label: name, number, markTo, revealed: revealedBy(reveal, node.from, markTo) });
        return;
      }
    }
  });
  return out;
}

// ── 装饰 ─────────────────────────────────────────────────────────────

const lineCache = new Map<string, Decoration>();
const lineDeco = (cls: string) => {
  let d = lineCache.get(cls);
  if (!d) lineCache.set(cls, (d = Decoration.line({ class: cls })));
  return d;
};
const langMark = Decoration.mark({ class: 'cm-td-code-lang' });
const prefixDeco = Decoration.replace({ widget: new FencePrefixWidget() });
const placeholderDeco = Decoration.widget({ widget: new LanguagePlaceholderWidget(), side: 1 });

function eachLine(state: EditorState, from: number, to: number, f: (lineFrom: number) => void) {
  const doc = state.doc;
  for (let l = doc.lineAt(from); ; l = doc.line(l.number + 1)) {
    f(l.from);
    if (l.to >= to || l.number >= doc.lines) break;
  }
}

/** 代码块、编辑中的 mermaid：首行是可编辑的语言名（``` 隐藏），内容行成框，闭围栏收起 */
function codeDecorations(state: EditorState, out: Range<Decoration>[], kind: FenceKind, lang: string, active: boolean, f: FenceGeometry, info: InfoRange | null) {
  const activeCls = active ? ' cm-td-fence-active' : '';
  out.push(lineDeco(`cm-td-fence-line cm-td-fence-lang-line cm-td-fence-${kind}${activeCls}`).range(f.openFrom));
  const infoFrom = info ? info.from : f.openTo;
  if (infoFrom > f.openFrom) out.push(prefixDeco.range(f.openFrom, infoFrom));
  if (info && info.to > info.from) out.push(langMark.range(info.from, info.to));
  else if (active) out.push(placeholderDeco.range(f.openTo));
  bodyDecorations(state, out, kind, lang, activeCls, f);
}

function bodyDecorations(state: EditorState, out: Range<Decoration>[], kind: FenceKind, lang: string, activeCls: string, f: FenceGeometry) {
  if (f.bodyFrom !== null) {
    const id = kind === 'code' ? codeLanguageId(lang) : kind === 'frontmatter' ? 'yaml' : null;
    // 没写语言的代码块另有字体与行高（Muya 的 pre 没有 language- 类，prism 主题的规则不生效）
    const plain = kind === 'code' && !lang.trim() ? ' cm-td-fence-plain' : '';
    const cls = `cm-td-fence-line cm-td-fence-body cm-td-fence-${kind}${activeCls}${id ? ` language-${id}` : ''}${plain}`;
    eachLine(state, f.bodyFrom, f.bodyTo!, at => out.push(lineDeco(cls).range(at)));
  }
  const active = activeCls !== '';
  if (f.closeFrom !== null) out.push(Decoration.replace({ widget: new FenceWidget(FenceRole.Foot, kind, active), block: true }).range(f.closeFrom, f.closeTo!));
  else out.push(Decoration.widget({ widget: new FenceWidget(FenceRole.Foot, kind, active), block: true, side: 1 }).range(f.bodyTo!));
}

/** 公式块、front matter：开闭围栏都收起 */
function collapsedDecorations(state: EditorState, out: Range<Decoration>[], kind: FenceKind, active: boolean, f: FenceGeometry) {
  out.push(Decoration.replace({ widget: new FenceWidget(FenceRole.Head, kind, active), block: true }).range(f.openFrom, f.openTo));
  bodyDecorations(state, out, kind, '', active ? ' cm-td-fence-active' : '', f);
}

export function specToDecorations(s: BlockSpec, state: EditorState): Range<Decoration>[] {
  const out: Range<Decoration>[] = [];
  switch (s.kind) {
    case 'table':
      out.push(Decoration.replace({ widget: new TableWidget(s.src), block: true }).range(s.from, s.to));
      break;
    case 'math':
    case 'mermaid':
      if (!s.editing) {
        out.push(Decoration.replace({ widget: new DiagramWidget(s.kind, s.src, false), block: true }).range(s.from, s.to));
        break;
      }
      if (s.fence.bodyFrom !== null) {
        if (s.kind === 'math') collapsedDecorations(state, out, 'math', true, s.fence);
        else codeDecorations(state, out, 'mermaid', 'mermaid', true, s.fence, s.info);
      }
      out.push(Decoration.widget({ widget: new DiagramWidget(s.kind, s.src, true), block: true, side: 1 }).range(s.to));
      break;
    case 'code':
      codeDecorations(state, out, 'code', s.lang, s.active, s.fence, s.info);
      break;
    case 'frontmatter':
      collapsedDecorations(state, out, 'frontmatter', s.active, s.fence);
      break;
    case 'html':
      if (s.editing) eachLine(state, s.from, s.to, at => out.push(lineDeco('cm-td-html-src').range(at)));
      else out.push(Decoration.replace({ widget: new HtmlWidget(s.src), block: true }).range(s.from, s.to));
      break;
    case 'toc':
      if (s.editing) out.push(lineDeco('cm-td-toc-src').range(s.from));
      else out.push(Decoration.replace({ widget: new TocWidget(s.entries), block: true }).range(s.from, s.to));
      break;
    case 'footnote': {
      const lastLine = state.doc.lineAt(s.to);
      eachLine(state, s.from, s.to, at => {
        const first = at === s.from, last = at === lastLine.from;
        out.push(lineDeco(`cm-td-footnote${first ? ' cm-td-footnote-first' : ''}${last ? ' cm-td-footnote-last' : ''}`).range(at));
      });
      if (!s.revealed) out.push(Decoration.replace({ widget: new FootnoteLabelWidget(s.label, s.number) }).range(s.from, s.markTo));
      else out.push(Decoration.mark({ class: 'cm-td-footnote-src' }).range(s.from, s.markTo));
      out.push(Decoration.widget({ widget: new FootnoteBackWidget(s.label), side: 1 }).range(s.to));
      break;
    }
  }
  return out;
}

/** 向后兼容的单个装饰出口（旧单测与探针按块取第一个装饰） */
export function specToDecoration(s: BlockSpec, state: EditorState): Range<Decoration> {
  return specToDecorations(s, state)[0];
}

const globalOf = (s: BlockSpec): Range<GlobalBlock> | null => (s.kind === 'toc' || s.kind === 'footnote' ? extentOf(s) : null);

/** 把区间扩到覆盖它的顶层块边界（按行对齐）。 */
function expandToBlocks(state: EditorState, tree: Tree, from: number, to: number): [number, number] {
  let f = from, t = to;
  iterateTopBlocks(tree, from, to, n => {
    f = Math.min(f, state.doc.lineAt(n.from).from);
    t = Math.max(t, state.doc.lineAt(n.to).to);
  });
  return [f, t];
}

/**
 * 脏区间扩到稳定：新语法树里与它相交的顶层块、旧装饰所属的块（extents，已按变化映射）都要整块包进来。
 * 例如在代码块开围栏行里插入反引号，开围栏失效后原来的闭围栏变成新块的开围栏，后面一个旧块的开围栏又变成它的闭围栏——
 * 旧块伸到脏区间之外的内容行装饰只有靠 extents 才能找到并清掉。
 */
function expandFully(state: EditorState, tree: Tree, extents: RangeSet<GlobalBlock>, from: number, to: number): [number, number] {
  let f = from, t = to;
  for (;;) {
    [f, t] = expandToBlocks(state, tree, f, t);
    let nf = f, nt = t;
    extents.between(f, t, (x, y) => { if (x < nf) nf = x; if (y > nt) nt = y; });
    if (nf === f && nt === t) return [f, t];
    f = nf; t = nt;
  }
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
const outlineRev = (state: EditorState) => state.field(outlineField, false)?.revision ?? 0;
const footnoteRev = (state: EditorState) => state.field(footnoteField, false)?.revision ?? 0;

export function createBlockState(state: EditorState): BlockState {
  const tree = syntaxTree(state);
  const covered = coveredEnd(state, tree);
  const reveal = selectionPoints(state);
  const specs = scanBlocks(state, tree, 0, covered, covered, reveal);
  const decos: Range<Decoration>[] = [];
  const extents: Range<GlobalBlock>[] = [];
  const globals: Range<GlobalBlock>[] = [];
  for (const s of specs) {
    decos.push(...specToDecorations(s, state));
    extents.push(extentOf(s));
    const g = globalOf(s);
    if (g) globals.push(g);
  }
  return {
    decos: Decoration.set(decos, true), extents: RangeSet.of(extents, true), globals: RangeSet.of(globals, true), covered, reveal,
    outlineRev: outlineRev(state), footnoteRev: footnoteRev(state),
  };
}

/** 统计每次更新重扫的字符数，给性能探针与单测用 */
export const blockFieldStats = { updates: 0, rescannedChars: 0, lastRescan: 0 };

export function updateBlockState(value: BlockState, tr: Transaction): BlockState {
  if (isComposeTransaction(tr)) {
    if (!tr.docChanged) return value;
    return {
      ...value,
      decos: value.decos.map(tr.changes),
      extents: value.extents.map(tr.changes),
      globals: value.globals.map(tr.changes),
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
  const oRev = outlineRev(state), fRev = footnoteRev(state);
  const globalsStale = oRev !== value.outlineRev || fRev !== value.footnoteRev;
  if (!tr.docChanged && !revealChanged && newCovered <= value.covered && !globalsStale) return value;

  let globals = tr.docChanged ? value.globals.map(tr.changes) : value.globals;
  let extents = tr.docChanged ? value.extents.map(tr.changes) : value.extents;
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
  if (globalsStale) {
    globals.between(0, state.doc.length, (f, t, g) => {
      if ((g.kind === 'toc' && oRev !== value.outlineRev) || (g.kind === 'footnote' && fRev !== value.footnoteRev)) dirty.push([f, t]);
    });
  }

  let decos = tr.docChanged ? value.decos.map(tr.changes) : value.decos;
  const add: Range<Decoration>[] = [];
  const addExtents: Range<GlobalBlock>[] = [];
  const addGlobals: Range<GlobalBlock>[] = [];
  let rescanned = 0;
  // 事务内的同步解析有时间上限（@codemirror/language 的 Work.Apply），编辑后语法树可能比上次短。
  // 旧覆盖范围内、新语法树之后的装饰是按变化映射过来的旧结果，只要不落在脏区间里就仍然正确，
  // 所以覆盖终点保留到第一个越过新语法树的脏区间为止，免得后台解析追回来时整段重扫。
  let keep = mappedCovered;
  // 先各自扩到稳定再合并；合并后的区间若又碰到别的块，再扩一轮（区间只增不减，很快收敛）
  let ranges = mergeRanges(dirty.map(([a, b]) => expandFully(state, tree, extents, a, b)));
  for (let changed = true; changed; ) {
    const next = mergeRanges(ranges.map(([a, b]) => expandFully(state, tree, extents, a, b)));
    changed = next.length !== ranges.length || next.some((r, i) => r[0] !== ranges[i][0] || r[1] !== ranges[i][1]);
    ranges = next;
  }
  for (const [f, t] of ranges) {
    const outside = (x: number, y: number) => y < f || x > t;
    decos = decos.update({ filterFrom: f, filterTo: t, filter: outside });
    extents = extents.update({ filterFrom: f, filterTo: t, filter: outside });
    globals = globals.update({ filterFrom: f, filterTo: t, filter: outside });
    if (f <= newCovered) {
      for (const s of scanBlocks(state, tree, f, t, newCovered, reveal)) {
        add.push(...specToDecorations(s, state));
        addExtents.push(extentOf(s));
        const g = globalOf(s);
        if (g) addGlobals.push(g);
      }
      rescanned += Math.min(t, newCovered) - f;
    }
    if (t > newCovered) keep = Math.min(keep, f);
  }
  if (add.length) decos = decos.update({ add, sort: true });
  if (addExtents.length) extents = extents.update({ add: addExtents, sort: true });
  if (addGlobals.length) globals = globals.update({ add: addGlobals, sort: true });
  blockFieldStats.updates++;
  blockFieldStats.lastRescan = rescanned;
  blockFieldStats.rescannedChars += rescanned;
  return { decos, extents, globals, covered: Math.max(newCovered, keep), reveal, outlineRev: oRev, footnoteRev: fRev };
}

export const blockField = StateField.define<BlockState>({
  create: createBlockState,
  update: updateBlockState,
  provide: f => EditorView.decorations.from(f, v => v.decos),
});
