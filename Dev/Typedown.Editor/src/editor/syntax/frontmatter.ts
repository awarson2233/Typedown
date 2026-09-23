import type { BlockContext, Line, MarkdownConfig, Element } from '@lezer/markdown';
import { tags } from '@lezer/highlight';

/**
 * front matter 的块解析器：只在文档第一行是 `---` 时成立，到下一个 `---` 或 `...` 行结束。
 * 与 @codemirror/lang-yaml 的 yamlFrontmatter 产出同名的 Frontmatter 节点，但不经 parseMixed 包一层——
 * 实测那一层让 1 MB 文档每次按键都多走一遍全树（见 Tools/perf-probe 的对照）。YAML 内容暂不做嵌套高亮。
 */
const isFence = (text: string) => /^(---|\.\.\.)\s*$/.test(text);

const LOOKAHEAD = 64 * 1024;

/** 文首 `---` 之后在前 64 KB 内是否有闭合行（块解析器不能退回已消费的行，所以先预读原文判断） */
function hasClosingFence(cx: BlockContext): boolean {
  const input = (cx as unknown as { input: { length: number; read(from: number, to: number): string } }).input;
  const head = input.read(0, Math.min(input.length, LOOKAHEAD));
  return /\n(---|\.\.\.)[ \t]*(\n|$)/.test(head);
}

export function parseFrontmatter(cx: BlockContext, line: Line): boolean {
  // depth 是容器栈长度，顶层时为 1（只有 Document）
  if (cx.lineStart !== 0 || cx.depth !== 1 || !/^---\s*$/.test(line.text)) return false;
  // 未闭合的 `---` 不是 front matter，交给后面的解析器（分隔线 / setext 标题）
  if (!hasClosingFence(cx)) return false;
  const children: Element[] = [cx.elt('FrontmatterMark', 0, 3)];
  while (cx.nextLine()) {
    if (isFence(line.text)) {
      children.push(cx.elt('FrontmatterMark', cx.lineStart, cx.lineStart + 3));
      cx.nextLine();
      break;
    }
    if (line.text.length) children.push(cx.elt('FrontmatterContent', cx.lineStart, cx.lineStart + line.text.length));
  }
  cx.addElement(cx.elt('Frontmatter', 0, children[children.length - 1].to, children));
  return true;
}

export const FrontmatterExtension: MarkdownConfig = {
  defineNodes: [
    { name: 'Frontmatter', block: true },
    { name: 'FrontmatterMark', style: tags.processingInstruction },
    { name: 'FrontmatterContent', style: tags.monospace },
  ],
  parseBlock: [{ name: 'Frontmatter', parse: parseFrontmatter, before: 'HorizontalRule' }],
};
