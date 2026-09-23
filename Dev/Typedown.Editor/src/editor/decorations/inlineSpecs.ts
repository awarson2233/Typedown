import type { Line, Text } from '@codemirror/state';
import type { SyntaxNode, Tree } from '@lezer/common';
import { emojiFor } from '../../shared/emoji';
import { RENDERED_TAGS, htmlAttr, pairHtmlTags, type HtmlTag } from './inlineHtml';
import { linePrefix, type MarkerSpec } from './lineStructure';
import { adjacentPadding, gapLineHeight, lastLineOf } from './blockSpacing';
import { iterateTopBlocks } from '../syntax';

/**
 * 行内显形的决策层：给定语法树、文档、要覆盖的区间与「显形位置」（选区端点），
 * 产出与视图无关的纯数据描述；ViewPlugin 只负责把它们换成 Decoration。
 *
 * 规则（webview-wysiwyg-engine.md 第 1、4 节；wysiwyg-engine-survey.md 第 1、6.2 节）：
 * - 行内 span（强调、删除线、高亮、行内代码、链接、行内公式、转义、emoji、脚注引用、行内 HTML）：
 *   任一显形位置落在 [from, to]（含两端）则露出标记并变灰（`cm-td-syntax`），否则隐藏（replace，并登记 atomicRanges）
 *   或换成渲染结果的 widget（公式、emoji、脚注上标、图片、`<br>`）。
 * - 图片：渲染态整段换成图片 widget；显形态保留源码，图片 widget 挂在源码之后（图保留）。
 * - 行内公式显形时源码下方浮出渲染预览（与旧编辑器的弹出预览相同）。
 * - ATX 标题按 Vditor IR：显形位置在标题所在行内时 `#` 变灰，否则连同其后空白隐藏；标题字号按行无条件施加。
 * - 列表符号、任务框、引用 `>` 始终隐藏（lineStructure）：行首前缀整段换成列表符号或复选框 widget，
 *   没有符号的前缀（引用 `>`、续行缩进）直接隐藏；缩进层数与引用竖线由行装饰施加。
 * - 块级样式（标题字号、引用、列表缩进、段间空行）只看语法，不随显形变化，避免行高跳动。
 */

export type InlineWidgetSpec =
  | MarkerSpec
  | { type: 'inline-math'; src: string }
  /** 图片 widget（widgets/imageWidget.ts）：src 为源码原文，相对路径由页面内部解析 */
  | { type: 'image'; src: string; alt: string; title: string | null }
  | { type: 'emoji'; char: string }
  /** 脚注引用上标：有编号时显示编号，否则显示标签原文 */
  | { type: 'footnote-ref'; label: string; number: number | null }
  | { type: 'html-break' };

export type InlineSpec =
  | { kind: 'mark'; from: number; to: number; cls: string }
  | { kind: 'hide'; from: number; to: number }
  /** 替换区间的 widget，登记为原子范围 */
  | { kind: 'widget'; from: number; to: number; widget: InlineWidgetSpec }
  /** 不替换文字、插在某个位置的 widget（显形态图片、公式预览） */
  | { kind: 'point'; at: number; side: -1 | 1; widget: InlineWidgetSpec }
  /** 行内 HTML 的内容区：包进真实元素（tag 为白名单里的小写标签名，attrs 为属性原文，消毒在视图层做） */
  | { kind: 'element'; from: number; to: number; tag: string; attrs: string }
  | { kind: 'line'; at: number; cls: string; style: string };

export interface InlineSpecOptions {
  /** 脚注编号（按标签原文查）；不给或查不到时上标显示标签原文 */
  footnoteNumber?: (label: string) => number | undefined;
}

const SPAN_CLASS: Record<string, string> = {
  Emphasis: 'cm-td-em',
  StrongEmphasis: 'cm-td-strong',
  Strikethrough: 'cm-td-del',
  Highlight: 'cm-td-highlight',
  InlineCode: 'cm-td-code-inline',
};
const SPAN_MARK: Record<string, string> = {
  Emphasis: 'EmphasisMark',
  StrongEmphasis: 'EmphasisMark',
  Strikethrough: 'StrikethroughMark',
  Highlight: 'HighlightMark',
  InlineCode: 'CodeMark',
};
/** 在父节点处统一处理、遍历到时直接跳过的节点（行首前缀由 lineStructure 按行处理） */
const HANDLED_BY_PARENT = new Set([
  'EmphasisMark', 'StrikethroughMark', 'HighlightMark', 'CodeMark', 'LinkMark', 'LinkTitle', 'LinkLabel', 'HeaderMark',
  'QuoteMark', 'ListMark', 'TaskMarker', 'FootnoteReferenceMark', 'FootnoteLabel',
]);
/** 空行落在这些块里时是块的内容，不是块间距 */
const BLOCK_CONTENT = new Set(['FencedCode', 'CodeBlock', 'BlockMath', 'HTMLBlock', 'CommentBlock', 'ProcessingInstructionBlock', 'Frontmatter', 'Table']);

export const SYNTAX = 'cm-td-syntax';
/** 块间空行（段距）：高度按旧编辑器相邻两块的外边距折叠结果给出（blockSpacing.ts），默认是段落的 .5em */
export const GAP_LINE = 'cm-td-gap';

export interface Range { from: number; to: number }

export const revealedBy = (reveal: readonly number[], from: number, to: number) => {
  for (const p of reveal) if (p >= from && p <= to) return true;
  return false;
};

const isSpace = (c: string) => c === ' ' || c === '\t';

function children(node: SyntaxNode, name?: string): SyntaxNode[] {
  const out: SyntaxNode[] = [];
  for (let c = node.firstChild; c; c = c.nextSibling) if (!name || c.name === name) out.push(c);
  return out;
}

/** 链接或图片目标里的标题去掉两端的引号或括号 */
const unquote = (s: string) => (s.length >= 2 ? s.slice(1, -1) : s);

/** 块间空行：整行空白、不在代码类块里、行首没有要画的列表符号 */
function isGapLine(tree: Tree, line: Line, contentFrom: number): boolean {
  if (!/^[ \t]*$/.test(line.text.slice(contentFrom - line.from))) return false;
  for (let n: SyntaxNode | null = tree.resolveInner(contentFrom, 1); n; n = n.parent) if (BLOCK_CONTENT.has(n.name)) return false;
  return true;
}

export function buildInlineSpecs(doc: Text, tree: Tree, range: Range, reveal: readonly number[], options: InlineSpecOptions = {}): InlineSpec[] {
  const out: InlineSpec[] = [];
  const lines = new Map<number, { cls: Set<string>; style: string }>();
  const lineCls = (pos: number, cls: string | null, style?: string) => {
    const at = doc.lineAt(pos).from;
    let e = lines.get(at);
    if (!e) lines.set(at, (e = { cls: new Set(), style: '' }));
    if (cls) for (const c of cls.split(' ')) e.cls.add(c);
    if (style) e.style = e.style ? `${e.style};${style}` : style;
  };
  /** 对节点覆盖、且落在可见区间内的每一行调用 f */
  const eachLine = (from: number, to: number, f: (lineFrom: number, first: boolean, last: boolean) => void) => {
    const a = Math.max(from, range.from), b = Math.min(to, range.to);
    if (a > b) return;
    const firstLine = doc.lineAt(from).number, lastLine = doc.lineAt(to).number;
    for (let l = doc.lineAt(a); ; l = doc.line(l.number + 1)) {
      f(l.from, l.number === firstLine, l.number === lastLine);
      if (l.to >= b || l.number >= doc.lines) break;
    }
  };
  const mark = (from: number, to: number, cls: string) => { if (to > from) out.push({ kind: 'mark', from, to, cls }); };
  // ViewPlugin 的替换装饰不能跨行（CM6 的硬性限制）：跨行的标记（例如链接目标里有换行）退为变灰显示
  const crossesLine = (from: number, to: number) => doc.lineAt(from).to < to;
  const hide = (from: number, to: number) => {
    if (to <= from) return;
    if (crossesLine(from, to)) mark(from, to, SYNTAX);
    else out.push({ kind: 'hide', from, to });
  };
  /** 渲染态换成 widget；跨行时只能显示源码 */
  const replace = (from: number, to: number, widget: InlineWidgetSpec) => {
    if (crossesLine(from, to)) return false;
    out.push({ kind: 'widget', from, to, widget });
    return true;
  };
  /** 引用与列表项覆盖的可见行，遍历结束后逐行求前缀 */
  const nestedLines = new Set<number>();
  /** 已配对过行内 HTML 的父节点 */
  const htmlParents = new Set<string>();

  const htmlTags = (parent: SyntaxNode) => {
    const key = `::`;
    if (htmlParents.has(key)) return;
    htmlParents.add(key);
    const { pairs, single } = pairHtmlTags(doc, parent);
    for (const { open, close } of pairs) {
      if (!RENDERED_TAGS.has(open.name)) { mark(open.from, open.to, 'cm-td-html-tag'); mark(close.from, close.to, 'cm-td-html-tag'); continue; }
      if (close.from > open.to) out.push({ kind: 'element', from: open.to, to: close.from, tag: open.name, attrs: open.attrs });
      if (revealedBy(reveal, open.from, close.to)) { mark(open.from, open.to, 'cm-td-html-tag'); mark(close.from, close.to, 'cm-td-html-tag'); }
      else { hide(open.from, open.to); hide(close.from, close.to); }
    }
    for (const t of single) singleTag(t);
  };
  const singleTag = (t: HtmlTag) => {
    const shown = revealedBy(reveal, t.from, t.to);
    if (t.name === 'img' && t.kind === 'void') {
      const widget: InlineWidgetSpec = { type: 'image', src: htmlAttr(t.attrs, 'src') ?? '', alt: htmlAttr(t.attrs, 'alt') ?? '', title: htmlAttr(t.attrs, 'title') };
      if (shown || !replace(t.from, t.to, widget)) { mark(t.from, t.to, 'cm-td-html-tag'); out.push({ kind: 'point', at: t.to, side: 1, widget }); }
      return;
    }
    if (t.name === 'br' && t.kind === 'void' && !shown && replace(t.from, t.to, { type: 'html-break' })) return;
    mark(t.from, t.to, 'cm-td-html-tag');
  };

  tree.iterate({
    from: range.from,
    to: range.to,
    enter(ref) {
      const name = ref.name;
      if (ref.type.isTop || name === 'Body') return true;
      if (HANDLED_BY_PARENT.has(name)) return false;

      switch (name) {
        case 'Frontmatter':
          eachLine(ref.from, ref.to, at => lineCls(at, 'cm-td-frontmatter'));
          return false;
        case 'Table': {
          // 顶层表格由块组件 StateField 换成网格；嵌套在列表、引用里的表格暂按源码显示
          const node = ref.node;
          if (node.parent?.type.isTop) return false;
          return true;
        }
        case 'BlockMath':
          eachLine(ref.from, ref.to, at => lineCls(at, 'cm-td-math-src'));
          for (const m of children(ref.node, 'BlockMathMark')) mark(m.from, m.to, SYNTAX);
          return false;
        case 'FencedCode':
        case 'CodeBlock': {
          eachLine(ref.from, ref.to, (at, first, last) => {
            lineCls(at, 'cm-td-code');
            if (first) lineCls(at, 'cm-td-code-first');
            if (last) lineCls(at, 'cm-td-code-last');
          });
          for (const c of children(ref.node)) if (c.name === 'CodeMark' || c.name === 'CodeInfo') mark(c.from, c.to, SYNTAX);
          return false;
        }
        case 'HTMLBlock':
        case 'CommentBlock':
        case 'ProcessingInstructionBlock':
          eachLine(ref.from, ref.to, at => lineCls(at, 'cm-td-html'));
          return false;
        case 'Blockquote':
        case 'ListItem':
          eachLine(ref.from, ref.to, at => nestedLines.add(at));
          return true;
        case 'ATXHeading1': case 'ATXHeading2': case 'ATXHeading3':
        case 'ATXHeading4': case 'ATXHeading5': case 'ATXHeading6': {
          const level = name.charCodeAt(10) - 48;
          const line = doc.lineAt(ref.from);
          lineCls(line.from, `cm-td-h cm-td-h${level}`);
          const shown = revealedBy(reveal, line.from, line.to);
          const marks = children(ref.node, 'HeaderMark');
          marks.forEach((m, i) => {
            if (shown) { mark(m.from, m.to, SYNTAX); return; }
            if (i === 0 && m.from === ref.from) {
              let end = m.to;
              while (end < line.to && isSpace(doc.sliceString(end, end + 1))) end++;
              hide(m.from, end);
            } else {
              let start = m.from;
              while (start > line.from && isSpace(doc.sliceString(start - 1, start))) start--;
              hide(start, m.to);
            }
          });
          return true;
        }
        case 'SetextHeading1': case 'SetextHeading2': {
          const level = name.endsWith('1') ? 1 : 2;
          const underline = ref.node.getChild('HeaderMark');
          eachLine(ref.from, ref.to, at => {
            if (underline && at === doc.lineAt(underline.from).from) lineCls(at, 'cm-td-setext-underline');
            else lineCls(at, `cm-td-h cm-td-h${level}`);
          });
          if (underline) mark(underline.from, underline.to, SYNTAX);
          return true;
        }
        case 'HorizontalRule': {
          const line = doc.lineAt(ref.from);
          lineCls(line.from, 'cm-td-hr');
          if (revealedBy(reveal, line.from, line.to)) mark(ref.from, ref.to, SYNTAX);
          else hide(ref.from, ref.to);
          return false;
        }
        case 'Emphasis': case 'StrongEmphasis': case 'Strikethrough': case 'Highlight': case 'InlineCode': {
          const marks = children(ref.node, SPAN_MARK[name]);
          // 行内代码的底色只包内容，反引号留在框外（旧编辑器的 `<code>` 不含反引号）
          if (name === 'InlineCode' && marks.length >= 2) mark(marks[0].to, marks[marks.length - 1].from, SPAN_CLASS[name]);
          else mark(ref.from, ref.to, SPAN_CLASS[name]);
          const shown = revealedBy(reveal, ref.from, ref.to);
          for (const m of marks) {
            if (shown) mark(m.from, m.to, SYNTAX); else hide(m.from, m.to);
          }
          return name !== 'InlineCode';
        }
        case 'Link': {
          const node = ref.node;
          const marks = children(node, 'LinkMark');
          if (marks.length < 2) return true;
          const shown = revealedBy(reveal, ref.from, ref.to);
          mark(marks[0].to, marks[1].from, 'cm-td-link');
          if (shown) {
            for (const c of children(node)) {
              if (c.name === 'LinkMark' || c.name === 'LinkTitle' || c.name === 'LinkLabel') mark(c.from, c.to, SYNTAX);
              else if (c.name === 'URL') mark(c.from, c.to, 'cm-td-url');
            }
          } else {
            hide(marks[0].from, marks[0].to);
            hide(marks[1].from, ref.to);
          }
          return true;
        }
        case 'Autolink': {
          const marks = children(ref.node, 'LinkMark');
          mark(ref.from, ref.to, 'cm-td-link');
          const shown = revealedBy(reveal, ref.from, ref.to);
          for (const m of marks) { if (shown) mark(m.from, m.to, SYNTAX); else hide(m.from, m.to); }
          return false;
        }
        case 'Image': {
          const node = ref.node;
          const marks = children(node, 'LinkMark');
          const url = node.getChild('URL');
          // 引用式图片（`![alt][ref]`）要先找到定义才知道地址，暂按源码显示；`![alt]()` 是空图片（有括号、没有地址），显示空图片占位
          const emptyTarget = !url && marks.length >= 4 && doc.sliceString(marks[2].from, marks[2].to) === '(';
          if (marks.length < 2 || (!url && !emptyTarget)) { mark(ref.from, ref.to, 'cm-td-image-src'); return false; }
          const title = node.getChild('LinkTitle');
          const widget: InlineWidgetSpec = {
            type: 'image',
            src: url ? doc.sliceString(url.from, url.to) : '',
            alt: doc.sliceString(marks[0].to, marks[1].from),
            title: title ? unquote(doc.sliceString(title.from, title.to)) : null,
          };
          if (!revealedBy(reveal, ref.from, ref.to) && replace(ref.from, ref.to, widget)) return false;
          for (const c of children(node)) {
            if (c.name === 'LinkMark' || c.name === 'LinkTitle') mark(c.from, c.to, SYNTAX);
            else if (c.name === 'URL') mark(c.from, c.to, 'cm-td-image-src');
          }
          mark(marks[0].to, marks[1].from, 'cm-td-image-alt');
          out.push({ kind: 'point', at: ref.to, side: 1, widget });
          return false;
        }
        case 'InlineMath': {
          const marks = children(ref.node, 'InlineMathMark');
          if (marks.length < 2) return false;
          const src = doc.sliceString(marks[0].to, marks[1].from);
          if (revealedBy(reveal, ref.from, ref.to) || crossesLine(ref.from, ref.to)) {
            mark(ref.from, ref.to, 'cm-td-math-inline-src');
            for (const m of marks) mark(m.from, m.to, SYNTAX);
            if (src.trim()) out.push({ kind: 'point', at: ref.from, side: -1, widget: { type: 'inline-math', src } });
          } else {
            out.push({ kind: 'widget', from: ref.from, to: ref.to, widget: { type: 'inline-math', src } });
          }
          return false;
        }
        case 'Emoji': {
          const char = emojiFor(doc.sliceString(ref.from + 1, ref.to - 1));
          if (!char) return false;
          if (revealedBy(reveal, ref.from, ref.to)) {
            mark(ref.from, ref.from + 1, SYNTAX);
            mark(ref.from + 1, ref.to - 1, 'cm-td-emoji-name');
            mark(ref.to - 1, ref.to, SYNTAX);
          } else {
            replace(ref.from, ref.to, { type: 'emoji', char });
          }
          return false;
        }
        case 'FootnoteReference': {
          const labelNode = ref.node.getChild('FootnoteLabel');
          const label = labelNode ? doc.sliceString(labelNode.from, labelNode.to) : doc.sliceString(ref.from + 2, ref.to - 1);
          if (revealedBy(reveal, ref.from, ref.to) || !replace(ref.from, ref.to, { type: 'footnote-ref', label, number: options.footnoteNumber?.(label) ?? null })) {
            mark(ref.from, ref.to, 'cm-td-footnote-ref-src');
            for (const m of children(ref.node, 'FootnoteReferenceMark')) mark(m.from, m.to, SYNTAX);
          }
          return false;
        }
        case 'Comment':
          if (revealedBy(reveal, ref.from, ref.to)) mark(ref.from, ref.to, 'cm-td-html-tag');
          else hide(ref.from, ref.to);
          return false;
        case 'HTMLTag': {
          const parent = ref.node.parent;
          if (parent) htmlTags(parent);
          return false;
        }
        case 'Escape':
          if (revealedBy(reveal, ref.from, ref.to)) mark(ref.from, ref.from + 1, SYNTAX);
          else hide(ref.from, ref.from + 1);
          return false;
        case 'URL': {
          // 链接内的 URL 已在父节点处理，这里只给 GFM 裸链接上样式
          const p = ref.node.parent?.name;
          if (p !== 'Link' && p !== 'Image' && p !== 'Autolink') mark(ref.from, ref.to, 'cm-td-link');
          return false;
        }
      }
      return true;
    },
  });

  // 引用、列表的行首前缀：列表符号 widget 或整段隐藏；缩进层数、引用竖线、已完成任务按行施加
  const prefixEnds = new Map<number, number>();
  for (const at of nestedLines) {
    const line = doc.lineAt(at);
    const p = linePrefix(tree, doc, line);
    if (!p) continue;
    prefixEnds.set(at, p.end);
    if (p.end > line.from) {
      if (p.marker) out.push({ kind: 'widget', from: line.from, to: p.end, widget: p.marker });
      else hide(line.from, p.end);
    }
    let style = `--td-indent:${p.indent}`;
    if (p.quoteLevels.length) {
      style += `;background-image:${p.quoteLevels.map(() => 'var(--td-quote-bar)').join(',')}`;
      style += `;background-position:${p.quoteLevels.map(l => `calc(${l} * var(--td-indent-step) + var(--td-quote-bar-left)) 0`).join(',')}`;
    }
    lineCls(at, `cm-td-nest${p.quoteLevels.length ? ' cm-td-quote' : ''}${p.taskDone ? ' cm-td-task-done' : ''}`, style);
    if (!p.marker && isGapLine(tree, line, p.end)) lineCls(at, GAP_LINE);
  }
  // 顶层的块间空行：同一段连续空行共用一个高度（按前后两块的外边距算，见 blockSpacing.ts）
  const isTopGap = (l: Line) => !prefixEnds.has(l.from) && isGapLine(tree, l, l.from);
  let run: { last: number; style: string | undefined } | null = null;
  for (let l = doc.lineAt(range.from); ; l = doc.line(l.number + 1)) {
    if (isTopGap(l)) {
      if (!run || l.number > run.last) {
        let first = l.number, last = l.number;
        while (first > 1 && isTopGap(doc.line(first - 1))) first--;
        while (last < doc.lines && isTopGap(doc.line(last + 1))) last++;
        const h = gapLineHeight(doc, tree, first, last);
        run = { last, style: h ? `--td-gap-h:${h}` : undefined };
      }
      lineCls(l.from, GAP_LINE, run.style);
    }
    if (l.to >= range.to || l.number >= doc.lines) break;
  }
  // 中间没有空行的相邻顶层块（以及文档首块）：旧编辑器里两块之间的外边距加成文字行的内边距
  iterateTopBlocks(tree, range.from, range.to, ref => {
    const node = ref.node;
    const prev = node.prevSibling;
    if (prev ? lastLineOf(doc, prev).number + 1 !== doc.lineAt(node.from).number : node.from !== 0) return;
    const pad = adjacentPadding(doc, prev, node);
    if (pad) lineCls(pad.at, null, `padding-${pad.side}:${pad.value}`);
  });

  for (const [at, e] of lines) {
    out.push({ kind: 'line', at, cls: [...e.cls].join(' '), style: e.style });
  }
  return out;
}
