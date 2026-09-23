import { syntaxTree } from '@codemirror/language';
import type { EditorState } from '@codemirror/state';
import type { SyntaxNode } from '@lezer/common';
import { ENUMS, type BlockContext, type BlockKind, type InlineMark } from '../../bridge/protocol';

/**
 * 选区的语义描述：光标所在块的 BlockContext（段落菜单的勾选与可用状态由它投影）与选区覆盖的行内标记。
 * 只读已有的语法树，不强制解析：光标处还没解析时退回最小描述（段落、无标记）。
 * 判定规则对照旧页面的 menuState.js：只看最近的一层列表；代码、公式、HTML、front matter 是「代码类」块；
 * 表格里块命令整体禁用；同时命中两种以上时去掉「段落」。
 */

const HEADINGS: Record<string, BlockKind> = {
  ATXHeading1: 'heading1', ATXHeading2: 'heading2', ATXHeading3: 'heading3',
  ATXHeading4: 'heading4', ATXHeading5: 'heading5', ATXHeading6: 'heading6',
  SetextHeading1: 'heading1', SetextHeading2: 'heading2',
};
const CODE_LIKE: Record<string, BlockKind> = {
  FencedCode: 'codeBlock', CodeBlock: 'codeBlock', BlockMath: 'mathBlock', HTMLBlock: 'htmlBlock',
  CommentBlock: 'htmlBlock', ProcessingInstructionBlock: 'htmlBlock', Frontmatter: 'frontMatter',
};
/** 首尾各有一行定界符的代码类块：定界行上不算「代码行」 */
const FENCED = new Set(['FencedCode', 'BlockMath', 'Frontmatter']);
/** 叶子块：两端落在不同的叶子块里就是多块选区 */
const LEAF = new Set(['Paragraph', 'Table', 'HorizontalRule', 'LinkReference', ...Object.keys(HEADINGS), ...Object.keys(CODE_LIKE)]);

/** pos 处最内层的节点：先向右看，落在块间（只剩 Document）时再向左看，行尾也能归到所在块 */
function innermost(state: EditorState, pos: number): SyntaxNode {
  const tree = syntaxTree(state);
  const right = tree.resolveInner(pos, 1);
  if (right.parent && !right.type.isTop) return right;
  return tree.resolveInner(pos, -1);
}

function leafBlock(node: SyntaxNode | null): SyntaxNode | null {
  for (let n = node; n; n = n.parent) if (LEAF.has(n.name)) return n;
  return null;
}

export function blockContextAt(state: EditorState, from: number, to: number): BlockContext {
  const doc = state.doc;
  const start = innermost(state, from);
  const kinds = new Set<BlockKind>();
  let codeLike = false, codeLine = false, table = false, list = false, quote = false;
  let item: SyntaxNode | null = null;
  for (let n: SyntaxNode | null = start; n; n = n.parent) {
    const name = n.name;
    if (name === 'Paragraph') kinds.add('paragraph');
    else if (HEADINGS[name]) kinds.add(HEADINGS[name]);
    else if (CODE_LIKE[name]) {
      let kind = CODE_LIKE[name];
      if (name === 'FencedCode') {
        const info = n.getChild('CodeInfo');
        if (info && /^mermaid$/i.test(doc.sliceString(info.from, info.to).trim())) kind = 'mermaidDiagram';
      }
      kinds.add(kind);
      codeLike = true;
      const line = doc.lineAt(from).number;
      codeLine = !FENCED.has(name) || (line > doc.lineAt(n.from).number && line < doc.lineAt(n.to).number);
    } else if (name === 'Table') { kinds.add('table'); table = true; }
    else if (name === 'HorizontalRule') kinds.add('horizontalRule');
    else if (name === 'Blockquote' && !quote) { kinds.add('quote'); quote = true; }
    else if (name === 'ListItem' && !item) item = n;
    else if ((name === 'BulletList' || name === 'OrderedList') && !list) {
      list = true;
      if (name === 'OrderedList') kinds.add('orderedList');
      else kinds.add(item?.getChild('Task') ? 'taskList' : 'bulletList');
    }
  }
  // 空行（块与块之间）在 Typora 里就是一个空段落
  if (!kinds.size) kinds.add('paragraph');
  if (kinds.size >= 2) kinds.delete('paragraph');
  let multipleBlocks = false;
  if (from !== to) {
    const a = leafBlock(start), b = leafBlock(innermost(state, to));
    multipleBlocks = a?.from !== b?.from || (!a && doc.lineAt(from).number !== doc.lineAt(to).number);
  }
  return {
    kinds: ENUMS.BlockKind.filter(k => kinds.has(k)),
    multipleBlocks,
    codeLike,
    codeLine: codeLike && codeLine,
    blockCommandsDisabled: table,
  };
}

const MARKS: Record<string, InlineMark> = {
  StrongEmphasis: 'strong', Emphasis: 'emphasis', Strikethrough: 'strikethrough', Highlight: 'highlight',
  InlineCode: 'inlineCode', InlineMath: 'inlineMath', Link: 'link', Autolink: 'link', Image: 'image',
};

/**
 * 选区覆盖的行内标记：空选区要求光标严格在标记内部（紧贴 `**` 外侧不算），非空选区要求整段落在标记里。
 * 下划线（`<u>` 标签对）不在语法树里成对出现，暂不识别。
 */
export function inlineMarksAt(state: EditorState, from: number, to: number): InlineMark[] {
  const found = new Set<InlineMark>();
  for (let n: SyntaxNode | null = syntaxTree(state).resolveInner(from, 1); n; n = n.parent) {
    const mark = MARKS[n.name];
    if (!mark) continue;
    if (from === to ? n.from < from && from < n.to : n.from <= from && to <= n.to) found.add(mark);
  }
  return ENUMS.InlineMark.filter(m => found.has(m));
}
