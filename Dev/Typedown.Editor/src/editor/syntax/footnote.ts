import type { BlockContext, InlineContext, LeafBlock, LeafBlockParser, Line, MarkdownConfig } from '@lezer/markdown';
import { tags } from '@lezer/highlight';

/**
 * 脚注（GFM 写法）：
 * - 行内引用 `[^label]`：FootnoteReference，子节点 FootnoteReferenceMark（`[^` 与 `]`）、FootnoteLabel；
 * - 定义 `[^label]: 内容`：FootnoteDefinition，子节点 FootnoteDefinitionMark（`[^` 与 `]:`）、FootnoteLabel，其后是内容的行内节点。
 * 定义按段落解析（叶块）：从段落开头的 `[^label]:` 起，到空行、能打断段落的块或下一个脚注定义为止；
 * 缩进的多段落脚注不支持（续段按普通缩进内容解析）。标签里不能有空白、`^`、方括号。
 */

const LABEL = /^\[\^([^\s\]^[]+)\]:/;

/**
 * 脚注 label 的规范化（CommonMark 的 link label 匹配规则）：去掉首尾空白、连续空白合并为一个空格、大小写折叠。
 * 大小写折叠用「小写 → 大写 → 小写」近似 Unicode case fold（与 commonmark.js 的 normalizeReference 相同，ẞ、ß、ss 能对上）。
 * 编号表（state/footnotes.ts）以它的结果为键；行内上标查编号前先调用它。
 */
export function normalizeFootnoteLabel(label: string): string {
  return label.trim().replace(/\s+/g, ' ').toLowerCase().toUpperCase().toLowerCase();
}

/** 行内 `[^label]`；`]` 后紧跟 `:` 的不算（那是行首定义的写法，由叶块解析器处理） */
export function parseFootnoteReference(cx: InlineContext, next: number, pos: number): number {
  if (next !== 91 /* [ */ || cx.char(pos + 1) !== 94 /* ^ */) return -1;
  let i = pos + 2;
  for (; i < cx.end; i++) {
    const c = cx.char(i);
    if (c === 93 /* ] */) break;
    if (c === 32 || c === 9 || c === 10 || c === 91 || c === 94) return -1;
  }
  if (i >= cx.end || i === pos + 2) return -1;
  return cx.addElement(cx.elt('FootnoteReference', pos, i + 1, [
    cx.elt('FootnoteReferenceMark', pos, pos + 2),
    cx.elt('FootnoteLabel', pos + 2, i),
    cx.elt('FootnoteReferenceMark', i, i + 1),
  ]));
}

class FootnoteDefinitionParser implements LeafBlockParser {
  nextLine(_cx: BlockContext, _line: Line, leaf: LeafBlock): boolean {
    // 链接引用定义的叶块解析器也会接下 `[^x]: …`（标签 `^x` 在 CommonMark 里合法），这里只留自己
    const self = leaf.parsers.indexOf(this);
    if (self >= 0 && leaf.parsers.length > 1) leaf.parsers.splice(0, leaf.parsers.length, this);
    return false;
  }

  finish(cx: BlockContext, leaf: LeafBlock): boolean {
    const m = LABEL.exec(leaf.content);
    if (!m) return false;
    const start = leaf.start, labelEnd = start + 2 + m[1].length, markEnd = start + m[0].length;
    cx.addLeafElement(leaf, cx.elt('FootnoteDefinition', start, start + leaf.content.length, [
      cx.elt('FootnoteDefinitionMark', start, start + 2),
      cx.elt('FootnoteLabel', start + 2, labelEnd),
      cx.elt('FootnoteDefinitionMark', labelEnd, markEnd),
      ...cx.parser.parseInline(leaf.content.slice(m[0].length), markEnd),
    ]));
    return true;
  }
}

const startsDefinition = (line: Line) => line.indent - line.baseIndent < 4 && LABEL.test(line.text.slice(line.pos));

export const FootnoteExtension: MarkdownConfig = {
  defineNodes: [
    { name: 'FootnoteDefinition', block: true },
    { name: 'FootnoteDefinitionMark', style: tags.processingInstruction },
    { name: 'FootnoteLabel', style: tags.labelName },
    { name: 'FootnoteReference', style: tags.special(tags.link) },
    { name: 'FootnoteReferenceMark', style: tags.processingInstruction },
  ],
  parseBlock: [{
    name: 'FootnoteDefinition',
    leaf: (_cx, leaf) => (LABEL.test(leaf.content) ? new FootnoteDefinitionParser() : null),
    // 下一个脚注定义结束当前定义（普通段落不受影响）
    endLeaf: (_cx, line, leaf) => leaf.parsers.some(p => p instanceof FootnoteDefinitionParser) && startsDefinition(line),
    before: 'LinkReference',
  }],
  parseInline: [{ name: 'FootnoteReference', parse: parseFootnoteReference, before: 'Link' }],
};
