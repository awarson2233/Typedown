import { EditorSelection, EditorState, type Extension, type Range } from '@codemirror/state';
import { Decoration, EditorView, ViewPlugin, type DecorationSet, type ViewUpdate } from '@codemirror/view';
import { syntaxTree } from '@codemirror/language';
import { buildInlineSpecs } from './inlineSpecs';
import { inlineWidget } from '../widgets/inlineWidgets';
import { hasRefresh, isComposeTransaction, revealFrozen } from './revealState';
import { canonicalHead, whitespaceInsertPos } from './canonical';

const hideDeco = Decoration.replace({});
const markCache = new Map<string, Decoration>();
const markDeco = (cls: string) => {
  let d = markCache.get(cls);
  if (!d) markCache.set(cls, (d = Decoration.mark({ class: cls })));
  return d;
};
const lineCache = new Map<string, Decoration>();
const lineDeco = (cls: string, quoteDepth: number) => {
  const key = cls + '|' + quoteDepth;
  let d = lineCache.get(key);
  if (!d) {
    lineCache.set(key, (d = Decoration.line({
      class: cls,
      attributes: quoteDepth ? { style: `--td-quote-depth:${quoteDepth}` } : undefined,
    })));
  }
  return d;
};

const selectionPoints = (state: EditorState) => {
  const pts: number[] = [];
  for (const r of state.selection.ranges) { pts.push(r.head); if (r.anchor !== r.head) pts.push(r.anchor); }
  return pts;
};

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
    this.decorations = this.decorations.map(u.changes);
    this.atomic = this.atomic.map(u.changes);
    this.reveal = this.reveal.map(p => u.changes.mapPos(p));
  }

  build(view: EditorView) {
    const { state } = view;
    const ranges = view.visibleRanges;
    if (!ranges.length) return { decorations: Decoration.none, atomic: Decoration.none };
    const specs = buildInlineSpecs(state.doc, syntaxTree(state), { from: ranges[0].from, to: ranges[ranges.length - 1].to }, this.reveal);
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
        case 'line': decos.push(lineDeco(s.cls, s.quoteDepth).range(s.at)); break;
      }
    }
    return { decorations: Decoration.set(decos, true), atomic: Decoration.set(atomic, true) };
  }
}

export const inlineReveal = ViewPlugin.fromClass(InlineReveal, { decorations: v => v.decorations });

/** 选区规范化：只作用于不改文本、非组字的选区事务。 */
const canonicalSelection = EditorState.transactionFilter.of(tr => {
  if (!tr.selection || tr.docChanged || isComposeTransaction(tr)) return tr;
  const tree = syntaxTree(tr.startState);
  const doc = tr.startState.doc;
  const prev = tr.startState.selection;
  const sel = tr.selection;
  let changed = false;
  const ranges = sel.ranges.map((r, i) => {
    const prevHead = (prev.ranges[i] ?? prev.main).head;
    const head = canonicalHead(doc, tree, prevHead, r.head);
    if (head === r.head) return r;
    changed = true;
    return r.empty ? EditorSelection.cursor(head, r.assoc) : EditorSelection.range(r.anchor, head);
  });
  if (!changed) return tr;
  return [tr, { selection: EditorSelection.create(ranges, sel.mainIndex), sequential: true }];
});

/** 在强调边缘键入空白时落到标记外。 */
const whitespaceOutside = EditorView.inputHandler.of((view, from, to, text) => {
  if (from !== to || view.composing || !/^[ \t　]+$/.test(text)) return false;
  const target = whitespaceInsertPos(syntaxTree(view.state), from);
  if (target === from) return false;
  view.dispatch({ changes: { from: target, insert: text }, selection: { anchor: target + text.length }, userEvent: 'input.type' });
  return true;
});

export const inlineRevealExtension: Extension = [
  inlineReveal,
  EditorView.atomicRanges.of(view => view.plugin(inlineReveal)?.atomic ?? Decoration.none),
  canonicalSelection,
  whitespaceOutside,
];
