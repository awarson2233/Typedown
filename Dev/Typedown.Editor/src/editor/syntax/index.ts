import { markdown, markdownKeymap, commonmarkLanguage } from '@codemirror/lang-markdown';
import { yamlFrontmatter } from '@codemirror/lang-yaml';
import { Language, LanguageSupport, LanguageDescription } from '@codemirror/language';
import { Prec } from '@codemirror/state';
import { keymap } from '@codemirror/view';
import { GFM, Emoji, type MarkdownExtension, type MarkdownParser } from '@lezer/markdown';
import type { SyntaxNodeRef, Tree } from '@lezer/common';
import { MathExtension } from './math';
import { HighlightExtension } from './highlight';
import { CjkEmphasisExtension } from './cjkEmphasis';
import { FrontmatterExtension } from './frontmatter';
import { FootnoteExtension } from './footnote';
import { TocExtension } from './toc';

/**
 * 语法：CommonMark + GFM（表格、任务列表、删除线、自动链接）+ emoji + 自有扩展。
 * 不用 lang-markdown 的 markdownLanguage，因为它带着上下标（W4 已砍）。
 */
export const markdownExtensions: MarkdownExtension[] = [GFM, Emoji, MathExtension, HighlightExtension, CjkEmphasisExtension,
  FootnoteExtension, TocExtension];

export interface SyntaxOptions {
  /**
   * 围栏代码的嵌套语言。给出时走 lang-markdown 的 markdown()，它总会用 parseMixed 包一层（围栏代码与 HTML 的嵌套解析）；
   * 不给时直接用 CommonMark 解析器建 Language，没有 parseMixed。两者的按键开销见 Tools/perf-probe 的对照。
   */
  codeLanguages?: readonly LanguageDescription[] | ((info: string) => LanguageDescription | null);
  /**
   * front matter：'native'（默认）为自有块解析器；'yaml' 为 @codemirror/lang-yaml 的 yamlFrontmatter（parseMixed 包一层，YAML 有高亮）；
   * false 不识别。
   */
  frontmatter?: 'native' | 'yaml' | false;
}

export function markdownSupport(options: SyntaxOptions = {}): LanguageSupport {
  const fm = options.frontmatter ?? 'native';
  const extensions = fm === 'native' ? [...markdownExtensions, FrontmatterExtension] : markdownExtensions;
  let md: LanguageSupport;
  if (options.codeLanguages) {
    md = markdown({
      base: commonmarkLanguage,
      extensions,
      codeLanguages: typeof options.codeLanguages === 'function' ? options.codeLanguages : [...options.codeLanguages],
      // 粘贴 URL 变链接属于 C3 的剪贴板命令，这里先关掉，避免粘贴改写选区文本
      pasteURLAsLink: false,
      completeHTMLTags: false,
    });
  } else {
    // 与 lang-markdown 的 mkLang 相同的构造（同一个 language data facet，markdownKeymap 的命令照常识别），只是不加 parseCode
    const parser = (commonmarkLanguage.parser as MarkdownParser).configure(extensions);
    md = new LanguageSupport(new Language(commonmarkLanguage.data, parser, [], 'markdown'), [Prec.high(keymap.of(markdownKeymap))]);
  }
  return fm === 'yaml' ? yamlFrontmatter({ content: md }) : md;
}

let cellParserCache: MarkdownParser | null = null;

/**
 * 表格单元格的解析器：GFM 表格对每个单元格的内容（去掉两侧空白）调用 parseInline，单元格里没有块级结构。
 * 这里去掉全部块级解析器，只剩段落：单行的单元格源码解析成「段落 + 行内节点」，行内部分与主文档表格里的解析结果相同
 * （`# a`、`- a`、`> a` 在单元格里都是普通文字）。非焦点单元格的渲染与焦点单元格的嵌套视图共用它。
 */
export function cellParser(): MarkdownParser {
  if (!cellParserCache) {
    const base = (commonmarkLanguage.parser as MarkdownParser).configure(markdownExtensions);
    // blockNames 是解析器的运行时字段（d.ts 未导出），按名字去掉已注册的全部块级解析器，扩展新增的块也一并去掉
    const names = (base as unknown as { blockNames: readonly string[] }).blockNames;
    cellParserCache = base.configure({ remove: [...names] });
  }
  return cellParserCache;
}

/** 单元格嵌套视图的语言：cellParser 建的 Language，不带 markdownKeymap（回车、列表续写在单元格里没有意义） */
export function cellLanguage(): LanguageSupport {
  return new LanguageSupport(new Language(commonmarkLanguage.data, cellParser(), [], 'markdown'));
}

/**
 * 遍历 markdown 顶层块（Document 的直接子节点）。front matter 包装层的 Document 与 Body 会被穿过，
 * Frontmatter 节点本身作为一个顶层块交给回调。
 */
export function iterateTopBlocks(tree: Tree, from: number, to: number, f: (node: SyntaxNodeRef) => void): void {
  tree.iterate({
    from, to,
    enter(node) {
      if (node.type.isTop || node.name === 'Body') return true;
      f(node);
      return false;
    },
  });
}
