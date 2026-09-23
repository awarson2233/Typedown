import { EditorSelection, EditorState, Facet, Prec, type Extension, type Range, type SelectionRange } from '@codemirror/state';
import { Decoration, EditorView, ViewPlugin, keymap, type Command, type DecorationSet, type ViewUpdate } from '@codemirror/view';
import { syntaxTree } from '@codemirror/language';
import DOMPurify from 'dompurify';
import { GAP_LINE, buildInlineSpecs, type InlineSpecOptions } from './inlineSpecs';
import { inlineWidget } from '../widgets/inlineWidgets';
import { hasRefresh, isComposeTransaction, revealFrozen } from './revealState';
import { canonicalHead, whitespaceInsertPos, type SelectionOrigin } from './canonical';
import { linkAt } from './links';
import { normalizeFootnoteLabel } from '../syntax/footnote';

/**
 * 脚注编号的来源（按引用首次出现的顺序编号，由脚注语法一侧提供；键为 normalizeFootnoteLabel 规范化后的标签）。没有提供时脚注上标显示标签原文。
 * 接法：`footnoteNumberSource.of(footnoteNumbers)`。
 */
export const footnoteNumberSource = Facet.define<(state: EditorState) => ReadonlyMap<string, number>, ((state: EditorState) => ReadonlyMap<string, number>) | null>({
  combine: v => v[0] ?? null,
});

/** Ctrl+单击链接时调用（页面装配处接到 view.openLink）；没有提供时 Ctrl+单击按普通单击处理。 */
export const openLinkHandler = Facet.define<(uri: string) => void, ((uri: string) => void) | null>({
  combine: v => v[0] ?? null,
});

const hideDeco = Decoration.replace({});
const markCache = new Map<string, Decoration>();
const markDeco = (cls: string) => {
  let d = markCache.get(cls);
  if (!d) markCache.set(cls, (d = Decoration.mark({ class: cls })));
  return d;
};
const lineCache = new Map<string, Decoration>();
const lineDeco = (cls: string, style: string) => {
  const key = cls + '|' + style;
  let d = lineCache.get(key);
  if (!d) lineCache.set(key, (d = Decoration.line({ class: cls, attributes: style ? { style } : undefined })));
  return d;
};

/** 行内 HTML 内容区的真实元素：属性经 DOMPurify 消毒，按「标签 + 属性原文」缓存 */
const elementCache = new Map<string, Decoration>();
const DROPPED_ATTRS = new Set(['id', 'contenteditable', 'tabindex', 'draggable', 'spellcheck']);
export function elementDeco(tag: string, attrs: string): Decoration {
  const key = tag + ' ' + attrs;
  let d = elementCache.get(key);
  if (d) return d;
  const attributes: Record<string, string> = {};
  const frag = DOMPurify.sanitize(`<${tag}${attrs}></${tag}>`, { RETURN_DOM_FRAGMENT: true });
  const el = frag.firstElementChild;
  if (el && el.localName === tag) {
    for (const a of Array.from(el.attributes)) if (!DROPPED_ATTRS.has(a.name)) attributes[a.name] = a.value;
  }
  attributes.class = attributes.class ? `cm-td-html-inline ${attributes.class}` : 'cm-td-html-inline';
  elementCache.set(key, (d = Decoration.mark({ tagName: tag, attributes })));
  return d;
}

const selectionPoints = (state: EditorState) => {
  const pts: number[] = [];
  for (const r of state.selection.ranges) { pts.push(r.head); if (r.anchor !== r.head) pts.push(r.anchor); }
  return pts;
};

const isGapDeco = (d: Decoration) => typeof d.spec.class === 'string' && d.spec.class.split(' ').includes(GAP_LINE);

/** 行内显形 ViewPlugin：只遍历视口；组字与鼠标冻结期间只映射。 */
class InlineReveal {
  decorations: DecorationSet;
  atomic: DecorationSet;
  /** 用于显形判断的位置（冻结与组字期间按变化映射，不跟随选区） */
  reveal: number[];
  stale = false;

  constructor(view: EditorView) {
    this.reveal = selectionPoints(view.state);
    ({ decorations: this.decorations, atomic: this.atomic } = this.build(view));
  }

  update(u: ViewUpdate) {
    const composing = u.view.composing || u.transactions.some(isComposeTransaction);
    const frozen = u.state.field(revealFrozen);
    if (composing) {
      this.map(u);
      this.stale = true;
      return;
    }
    const refresh = u.transactions.some(hasRefresh);
    if (!frozen && (u.selectionSet || refresh)) this.reveal = selectionPoints(u.state);
    else if (u.docChanged) this.reveal = this.reveal.map(p => u.changes.mapPos(p));
    const treeChanged = syntaxTree(u.state) !== syntaxTree(u.startState);
    if (u.docChanged || u.viewportChanged || u.selectionSet || refresh || treeChanged || this.stale) {
      this.stale = false;
      ({ decorations: this.decorations, atomic: this.atomic } = this.build(u.view));
    }
  }

  map(u: ViewUpdate) {
    if (!u.docChanged) return;
    this.decorations = unGapChangedLines(this.decorations.map(u.changes), u);
    this.atomic = this.atomic.map(u.changes);
    this.reveal = this.reveal.map(p => u.changes.mapPos(p));
  }

  build(view: EditorView) {
    const { state } = view;
    const ranges = view.visibleRanges;
    if (!ranges.length) return { decorations: Decoration.none, atomic: Decoration.none };
    const numbers = state.facet(footnoteNumberSource)?.(state);
    // 编号表的键是规范化后的标签；上标没有编号时显示标签原文（inlineSpecs 负责）
    const options: InlineSpecOptions = numbers ? { footnoteNumber: label => numbers.get(normalizeFootnoteLabel(label)) } : {};
    const specs = buildInlineSpecs(state.doc, syntaxTree(state), { from: ranges[0].from, to: ranges[ranges.length - 1].to }, this.reveal, options);
    const decos: Range<Decoration>[] = [];
    const atomic: Range<Decoration>[] = [];
    for (const s of specs) {
      switch (s.kind) {
        case 'mark': decos.push(markDeco(s.cls).range(s.from, s.to)); break;
        case 'hide': { const r = hideDeco.range(s.from, s.to); decos.push(r); atomic.push(r); break; }
        case 'widget': {
          const r = Decoration.replace({ widget: inlineWidget(s.widget) }).range(s.from, s.to);
          decos.push(r); atomic.push(r); break;
        }
        case 'point': decos.push(Decoration.widget({ widget: inlineWidget(s.widget, true), side: s.side }).range(s.at)); break;
        case 'element': decos.push(elementDeco(s.tag, s.attrs).range(s.from, s.to)); break;
        case 'line': decos.push(lineDeco(s.cls, s.style).range(s.at)); break;
      }
    }
    return { decorations: Decoration.set(decos, true), atomic: Decoration.set(atomic, true) };
  }
}

/**
 * 组字期间装饰只映射；但在块间空行（压矮的行）上开始组字时，组字文字会溢出到下一行之上。
 * 被改动的行不再是空行，所以只把这些行的「空行」类名去掉（只改行元素的 class，不碰组字所在的文本节点），组字结束后照常重算。
 */
function unGapChangedLines(decos: DecorationSet, u: ViewUpdate): DecorationSet {
  const doc = u.state.doc;
  const touched: [number, number][] = [];
  u.changes.iterChangedRanges((_fa, _ta, fromB, toB) => touched.push([doc.lineAt(fromB).from, doc.lineAt(toB).from]));
  const add: Range<Decoration>[] = [];
  for (const [from, to] of touched) {
    decos.between(from, to, (f, _t, d) => {
      if (f < from || f > to || !isGapDeco(d)) return;
      const cls = (d.spec.class as string).split(' ').filter(c => c !== GAP_LINE).join(' ');
      if (cls) add.push(lineDeco(cls, (d.spec.attributes as { style?: string } | undefined)?.style ?? '').range(f));
    });
  }
  if (!touched.length) return decos;
  let out = decos;
  for (const [from, to] of touched) out = out.update({ filterFrom: from, filterTo: to, filter: (f, _t, d) => !(f >= from && f <= to && isGapDeco(d)) });
  return add.length ? out.update({ add, sort: true }) : out;
}

export const inlineReveal = ViewPlugin.fromClass(InlineReveal, { decorations: v => v.decorations });

/** Home 的选区事务带这个用户事件，选区规范化据此把它与方向键区分开 */
const HOME_EVENT = 'select.lineBoundary';

function selectionOrigin(tr: { isUserEvent(e: string): boolean }): SelectionOrigin {
  if (tr.isUserEvent('select.pointer')) return 'pointer';
  if (tr.isUserEvent(HOME_EVENT)) return 'lineBoundary';
  return 'keyboard';
}

/** 选区规范化：只作用于不改文本、非组字的选区事务。 */
const canonicalSelection = EditorState.transactionFilter.of(tr => {
  if (!tr.selection || tr.docChanged || isComposeTransaction(tr)) return tr;
  const tree = syntaxTree(tr.startState);
  const doc = tr.startState.doc;
  const prev = tr.startState.selection;
  const sel = tr.selection;
  const origin = selectionOrigin(tr);
  let changed = false;
  const ranges = sel.ranges.map((r, i) => {
    const prevHead = (prev.ranges[i] ?? prev.main).head;
    const head = canonicalHead(doc, tree, prevHead, r.head, origin, r.empty);
    if (head === r.head) return r;
    changed = true;
    return r.empty ? EditorSelection.cursor(head, r.assoc) : EditorSelection.range(r.anchor, head);
  });
  if (!changed) return tr;
  return [tr, { selection: EditorSelection.create(ranges, sel.mainIndex), sequential: true }];
});

/** Home / Shift+Home：移到视觉行首（折行时是本段折行的起点），落进行首隐藏前缀时由选区规范化停在前缀末尾 */
const lineStart = (extend: boolean): Command => view => {
  const sel = view.state.selection;
  const move = (r: SelectionRange) => {
    const target = view.moveToLineBoundary(r, false);
    return extend ? EditorSelection.range(r.anchor, target.head, target.goalColumn) : target;
  };
  const next = EditorSelection.create(sel.ranges.map(move), sel.mainIndex);
  if (next.eq(sel)) return true;
  view.dispatch({ selection: next, scrollIntoView: true, userEvent: HOME_EVENT });
  return true;
};

/** 在强调边缘键入空白时落到标记外。 */
const whitespaceOutside = EditorView.inputHandler.of((view, from, to, text) => {
  if (from !== to || view.composing || !/^[ \t　]+$/.test(text)) return false;
  const target = whitespaceInsertPos(syntaxTree(view.state), from);
  if (target === from) return false;
  view.dispatch({ changes: { from: target, insert: text }, selection: { anchor: target + text.length }, userEvent: 'input.type' });
  return true;
});

/** Ctrl+单击打开链接（行内链接、尖括号自动链接、GFM 裸链接）；引用式链接要查定义，暂不支持。 */
const ctrlClickLink = EditorView.domEventHandlers({
  mousedown(e, view) {
    if (e.button !== 0 || !(e.ctrlKey || e.metaKey) || e.altKey || e.shiftKey) return false;
    const open = view.state.facet(openLinkHandler);
    if (!open) return false;
    const pos = view.posAtCoords({ x: e.clientX, y: e.clientY });
    if (pos === null) return false;
    const tree = syntaxTree(view.state);
    const link = linkAt(tree, view.state.doc, pos, 1) ?? linkAt(tree, view.state.doc, pos, -1);
    if (!link?.url) return false;
    e.preventDefault();
    open(link.url);
    return true;
  },
});

export const inlineRevealExtension: Extension = [
  inlineReveal,
  EditorView.atomicRanges.of(view => view.plugin(inlineReveal)?.atomic ?? Decoration.none),
  canonicalSelection,
  whitespaceOutside,
  ctrlClickLink,
  Prec.high(keymap.of([{ key: 'Home', run: lineStart(false), shift: lineStart(true) }])),
];
