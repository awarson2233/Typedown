import { StateEffect, type Range } from '@codemirror/state';
import { Decoration, ViewPlugin, type DecorationSet, type EditorView, type ViewUpdate } from '@codemirror/view';
import { syntaxTree } from '@codemirror/language';
import { highlightTree, tagHighlighter, tags as t } from '@lezer/highlight';
import { iterateTopBlocks } from '../syntax';
import { isComposeTransaction } from '../decorations/revealState';
import { findCodeLanguage, infoLanguageName } from './codeLanguage';

/**
 * 围栏代码的语法高亮：只对视口内的顶层代码块按需进行，不开全局嵌套解析（C1 实测 parseMixed 在 1 MB 文档上每键多 10–15 ms）。
 * 每个可见代码块单独用对应语言的解析器解析内容文本、highlightTree 出 token，结果按「语言 + 内容」缓存，
 * 所以按键时只有被编辑的那一个块重解析。高亮类名取 prism 的写法（`token keyword`），prism 主题基本原样可用。
 * 语言按需加载（legacy-modes），加载完成后发一个空效果让视口重建。超过 MAX_BLOCK 的块不高亮（每键整块重解析太贵）。
 */

export const MAX_BLOCK = 20000;

/** @lezer/highlight 标签 → prism 的 token 类名 */
const PRISM_SPECS = [
  { tag: [t.keyword, t.modifier, t.operatorKeyword, t.controlKeyword, t.definitionKeyword, t.moduleKeyword], class: 'token keyword' },
  { tag: [t.string, t.special(t.string), t.character, t.docString], class: 'token string' },
  { tag: t.regexp, class: 'token regex' },
  { tag: t.escape, class: 'token char' },
  { tag: [t.comment, t.lineComment, t.blockComment, t.docComment], class: 'token comment' },
  { tag: [t.number, t.integer, t.float], class: 'token number' },
  { tag: t.bool, class: 'token boolean' },
  { tag: [t.null, t.atom, t.constant(t.variableName), t.unit], class: 'token constant' },
  { tag: [t.typeName, t.className, t.definition(t.typeName)], class: 'token class-name' },
  { tag: t.namespace, class: 'token namespace' },
  { tag: [t.function(t.variableName), t.function(t.propertyName), t.function(t.definition(t.variableName)), t.macroName], class: 'token function' },
  { tag: [t.standard(t.variableName), t.self], class: 'token builtin' },
  { tag: [t.propertyName, t.definition(t.propertyName)], class: 'token property' },
  { tag: t.attributeName, class: 'token attr-name' },
  { tag: t.attributeValue, class: 'token attr-value' },
  { tag: t.tagName, class: 'token tag' },
  { tag: [t.operator, t.derefOperator, t.arithmeticOperator, t.logicOperator, t.bitwiseOperator, t.compareOperator, t.updateOperator, t.definitionOperator, t.typeOperator, t.controlOperator], class: 'token operator' },
  { tag: [t.punctuation, t.separator, t.bracket, t.angleBracket, t.squareBracket, t.paren, t.brace], class: 'token punctuation' },
  { tag: [t.meta, t.annotation, t.processingInstruction, t.documentMeta], class: 'token atrule' },
  { tag: [t.url, t.link], class: 'token url' },
  { tag: t.labelName, class: 'token symbol' },
  { tag: t.inserted, class: 'token inserted' },
  { tag: t.deleted, class: 'token deleted' },
  { tag: t.changed, class: 'token important' },
  { tag: t.heading, class: 'token important' },
  { tag: t.strong, class: 'token bold' },
  { tag: t.emphasis, class: 'token italic' },
];
export const prismHighlighter = tagHighlighter(PRISM_SPECS);

/**
 * YAML 按 prism 的 yaml 语法给类名（front matter 的颜色要与旧编辑器一致）：键是 `key atrule`，
 * 未加引号的标量 lezer 只标成 content，这里再按 prism 的正则分出日期、布尔、null、数字，其余不着色。
 */
const YAML_PLAIN = 'yaml-plain';
const yamlHighlighter = tagHighlighter([
  ...PRISM_SPECS,
  { tag: t.definition(t.propertyName), class: 'token key atrule' },
  { tag: t.content, class: YAML_PLAIN },
]);
const YAML_SCALARS: readonly [RegExp, string][] = [
  [/^\d{4}-\d\d?-\d\d?(?:(?:[Tt]|[ \t]+)\d\d?:\d{2}:\d{2}(?:\.\d*)?[ \t]*(?:Z|[-+]\d\d?(?::\d{2})?)?)?$|^\d\d?:\d{2}(?::\d{2}(?:\.\d*)?)?$/, 'token datetime number'],
  [/^(?:false|true)$/i, 'token boolean important'],
  [/^(?:null|~)$/i, 'token null important'],
  [/^[+-]?(?:0x[\da-f]+|0o[0-7]+|(?:\d[\d_]*(?:\.\d*)?|\.\d+)(?:e[+-]?\d+)?|\.inf|\.nan)$/i, 'token number'],
];
function yamlScalarClass(text: string): string | null {
  const s = text.trim();
  for (const [re, cls] of YAML_SCALARS) if (re.test(s)) return cls;
  return null;
}

/** 一个代码块内容的 token：相对内容起点的 [from, to, 类名] */
export type CodeToken = readonly [number, number, string];

const tokenCache = new Map<string, readonly CodeToken[]>();

/** 解析并高亮一段代码（语言已加载时）；未加载返回 null 并触发加载 */
export function highlightCode(info: string, text: string, onLoaded?: () => void): readonly CodeToken[] | null {
  const desc = findCodeLanguage(info);
  if (!desc) return [];
  if (!desc.support) {
    desc.load().then(() => onLoaded?.(), () => undefined);
    return null;
  }
  const key = desc.name + '\n' + text;
  let tokens = tokenCache.get(key);
  if (!tokens) {
    const out: CodeToken[] = [];
    const tree = desc.support.language.parser.parse(text);
    if (desc.name === 'YAML') {
      highlightTree(tree, yamlHighlighter, (from, to, cls) => {
        if (cls === YAML_PLAIN) {
          const c = yamlScalarClass(text.slice(from, to));
          if (c) out.push([from, to, c]);
        } else if (cls) out.push([from, to, cls]);
      });
    } else {
      highlightTree(tree, prismHighlighter, (from, to, cls) => { if (cls) out.push([from, to, cls]); });
    }
    tokens = out;
    if (tokenCache.size > 400) tokenCache.clear();
    tokenCache.set(key, tokens);
  }
  return tokens;
}

/** 语言按需加载完成后通知高亮插件重建 */
export const codeLanguageLoaded = StateEffect.define<null>();

const markCache = new Map<string, Decoration>();
const markDeco = (cls: string) => {
  let d = markCache.get(cls);
  if (!d) markCache.set(cls, (d = Decoration.mark({ class: cls })));
  return d;
};

function build(view: EditorView, onLoaded: () => void): DecorationSet {
  const { state } = view;
  const doc = state.doc;
  const tree = syntaxTree(state);
  const out: Range<Decoration>[] = [];
  const seen = new Set<number>();
  for (const { from, to } of view.visibleRanges) {
    iterateTopBlocks(tree, from, to, n => {
      // front matter 按 YAML 高亮（Muya 把它渲染成 pre.language-yaml，prism 着色）
      const fm = n.name === 'Frontmatter';
      if ((n.name !== 'FencedCode' && !fm) || seen.has(n.from)) return;
      seen.add(n.from);
      const info = fm ? null : n.node.getChild('CodeInfo');
      const lang = fm ? 'yaml' : info ? infoLanguageName(doc.sliceString(info.from, info.to)) : '';
      if (!lang || /^mermaid$/i.test(lang)) return;
      const first = doc.lineAt(n.from), last = doc.lineAt(n.to > n.from && doc.lineAt(n.to).from === n.to ? n.to - 1 : n.to);
      const closed = fm ? last.number > first.number : n.node.getChildren('CodeMark').length >= 2 && last.number > first.number;
      const bodyLast = closed ? last.number - 1 : last.number;
      if (bodyLast <= first.number) return;
      const bodyFrom = doc.line(first.number + 1).from, bodyTo = doc.line(bodyLast).to;
      if (bodyTo - bodyFrom > MAX_BLOCK) return;
      const tokens = highlightCode(lang, doc.sliceString(bodyFrom, bodyTo), onLoaded);
      if (tokens) for (const [f, t, cls] of tokens) out.push(markDeco(cls).range(bodyFrom + f, bodyFrom + t));
    });
  }
  return Decoration.set(out, true);
}

export const codeHighlight = ViewPlugin.fromClass(class {
  decorations: DecorationSet;
  private pending = false;
  private destroyed = false;
  /** 组字期间只映射，结束后补一次重建（与显形层相同，避免改动组字中的 DOM） */
  private stale = false;
  constructor(readonly view: EditorView) {
    this.decorations = build(view, this.onLoaded);
  }
  private readonly onLoaded = () => {
    if (this.pending || this.destroyed) return;
    this.pending = true;
    queueMicrotask(() => {
      this.pending = false;
      if (!this.destroyed) this.view.dispatch({ effects: codeLanguageLoaded.of(null) });
    });
  };
  update(u: ViewUpdate) {
    if (u.view.composing || u.transactions.some(isComposeTransaction)) {
      if (u.docChanged) this.decorations = this.decorations.map(u.changes);
      this.stale = true;
      return;
    }
    const loaded = u.transactions.some(tr => tr.effects.some(e => e.is(codeLanguageLoaded)));
    if (this.stale || u.docChanged || u.viewportChanged || loaded || syntaxTree(u.state) !== syntaxTree(u.startState)) {
      this.stale = false;
      this.decorations = build(u.view, this.onLoaded);
    }
  }
  destroy() { this.destroyed = true; }
}, { decorations: v => v.decorations });
