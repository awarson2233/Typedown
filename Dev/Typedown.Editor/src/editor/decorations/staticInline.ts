import type { EditorView } from '@codemirror/view';
import type { InlineSpec } from './inlineSpecs';
import { elementDeco } from './inlinePlugin';
import { inlineWidget } from '../widgets/inlineWidgets';

/**
 * 把行内显形的决策（inlineSpecs.buildInlineSpecs 的产出）画成静态 DOM，结构与 CM6 渲染同一组装饰的结果相同：
 * mark → `<span class>`，element → 消毒后的真实元素（elementDeco），hide → 不出现，widget / point → inlineWidget 的 DOM。
 * 规则只有 buildInlineSpecs 一份；这里只做「装饰 → DOM」这一步，给表格里的非焦点单元格用（单元格不是 CM6 的行，没有视图替它画）。
 * 行装饰（kind 'line'）不属于行内，忽略。
 *
 * 每个文本节点与 widget 元素记下它对应的源码区间，单击时把点中的 DOM 位置换回源码偏移（staticOffset）。
 */

interface Origin { readonly from: number; readonly to: number; readonly widget: boolean }
const origins = new WeakMap<Node, Origin>();

type WrapSpec = Extract<InlineSpec, { kind: 'mark' | 'element' }>;
type ReplaceSpec = Extract<InlineSpec, { kind: 'hide' | 'widget' }>;
type PointSpec = Extract<InlineSpec, { kind: 'point' }>;

function makeWrap(s: WrapSpec): HTMLElement {
  if (s.kind === 'mark') {
    const el = document.createElement('span');
    el.className = s.cls;
    return el;
  }
  const spec = elementDeco(s.tag, s.attrs).spec as { tagName: string; attributes: Record<string, string> };
  const el = document.createElement(spec.tagName);
  for (const [k, v] of Object.entries(spec.attributes)) el.setAttribute(k, v);
  return el;
}

export function renderInlineStatic(parent: HTMLElement, text: string, specs: readonly InlineSpec[], view: EditorView): void {
  const wraps: WrapSpec[] = [];
  const replaces: ReplaceSpec[] = [];
  const points: PointSpec[] = [];
  for (const s of specs) {
    if (s.kind === 'mark' || s.kind === 'element') { if (s.to > s.from) wraps.push(s); }
    else if (s.kind === 'hide' || s.kind === 'widget') { if (s.to > s.from) replaces.push(s); }
    else if (s.kind === 'point') points.push(s);
  }
  // 外层在前：起点小的在前，起点相同时长的在前（与 CM6 给重叠 mark 排嵌套的顺序一致）
  const order = wraps.map((w, i) => ({ w, i })).sort((a, b) => a.w.from - b.w.from || b.w.to - a.w.to || a.i - b.i).map(x => x.w);
  replaces.sort((a, b) => a.from - b.from || b.to - a.to);
  points.sort((a, b) => a.at - b.at || a.side - b.side);

  const bounds = new Set<number>([0, text.length]);
  for (const s of order) { bounds.add(s.from); bounds.add(s.to); }
  for (const s of replaces) { bounds.add(s.from); bounds.add(s.to); }
  for (const p of points) bounds.add(p.at);
  const cuts = [...bounds].filter(b => b >= 0 && b <= text.length).sort((a, b) => a - b);

  // 当前打开的包裹元素（与 order 中的项一一对应）
  const stack: { spec: WrapSpec; el: HTMLElement }[] = [];
  const top = () => (stack.length ? stack[stack.length - 1].el : parent);
  /** 把打开的包裹元素调整成 active（按 order 排好的列表）：保留公共前缀，关掉其余，再打开缺的 */
  const enter = (active: WrapSpec[]) => {
    let keep = 0;
    while (keep < stack.length && keep < active.length && stack[keep].spec === active[keep]) keep++;
    stack.length = keep;
    for (let i = keep; i < active.length; i++) {
      const el = makeWrap(active[i]);
      top().appendChild(el);
      stack.push({ spec: active[i], el });
    }
  };
  const covering = (from: number, to: number, strict: boolean) =>
    order.filter(w => (strict ? w.from < from && w.to > to : w.from <= from && w.to >= to));
  const emitWidget = (dom: HTMLElement, from: number, to: number) => {
    origins.set(dom, { from, to, widget: true });
    top().appendChild(dom);
  };

  let ri = 0, pi = 0;
  for (let ci = 0; ci < cuts.length; ci++) {
    const at = cuts[ci];
    // 这个位置上的点 widget（显形态的图片、公式预览）
    while (pi < points.length && points[pi].at < at) pi++;
    for (; pi < points.length && points[pi].at === at; pi++) {
      const p = points[pi];
      enter(covering(at, at, true));
      emitWidget(inlineWidget(p.widget, true).toDOM(view), at, at);
    }
    if (at >= text.length) break;
    while (ri < replaces.length && replaces[ri].to <= at) ri++;
    const r = ri < replaces.length && replaces[ri].from <= at ? replaces[ri] : null;
    if (r) {
      // 被替换的区间：从起点起整段跳过；widget 在起点处画一次
      if (r.from === at && r.kind === 'widget') {
        enter(covering(r.from, r.to, false));
        emitWidget(inlineWidget(r.widget).toDOM(view), r.from, r.to);
      }
      // 跳到替换区间终点所在的切点
      while (ci + 1 < cuts.length && cuts[ci + 1] < r.to) ci++;
      continue;
    }
    const next = cuts[ci + 1];
    enter(covering(at, next, false));
    const node = document.createTextNode(text.slice(at, next));
    origins.set(node, { from: at, to: next, widget: false });
    top().appendChild(node);
  }
}

function firstOrigin(n: Node): Origin | null {
  const o = origins.get(n);
  if (o) return o;
  for (let c = n.firstChild; c; c = c.nextSibling) { const r = firstOrigin(c); if (r) return r; }
  return null;
}
function lastOrigin(n: Node): Origin | null {
  const o = origins.get(n);
  if (o) return o;
  for (let c = n.lastChild; c; c = c.previousSibling) { const r = lastOrigin(c); if (r) return r; }
  return null;
}

/**
 * 静态渲染里的 DOM 位置（例如 caretPositionFromPoint 的结果）→ 源码偏移；位置不在 root 里时为 null。
 * 落在 widget 里时取 widget 的起点或终点（点在后半段时取终点）。
 */
export function staticOffset(root: HTMLElement, node: Node, offset: number): number | null {
  if (!root.contains(node)) return null;
  for (let n: Node | null = node; n && n !== root; n = n.parentNode) {
    const o = origins.get(n);
    if (!o) continue;
    if (!o.widget) return Math.min(o.from + offset, o.to);
    const len = n === node ? (node.nodeType === Node.TEXT_NODE ? (node as Text).length : node.childNodes.length) : 1;
    return n === node && offset * 2 >= len && len > 0 ? o.to : o.from;
  }
  // 元素里的子节点下标：取后一个子节点的起点，没有后一个时取前一个的终点
  const kids = node.childNodes;
  for (let i = offset; i < kids.length; i++) { const o = firstOrigin(kids[i]); if (o) return o.from; }
  for (let i = Math.min(offset, kids.length) - 1; i >= 0; i--) { const o = lastOrigin(kids[i]); if (o) return o.to; }
  const any = firstOrigin(root);
  return any ? any.from : 0;
}
