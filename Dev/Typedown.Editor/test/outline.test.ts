import { describe, expect, it } from 'vitest';
import { readdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { ChangeSet, EditorState, Text } from '@codemirror/state';
import { ensureSyntaxTree } from '@codemirror/language';
import { headingIds, headingIndexAt, outlineField, scanOutline, updateOutline, type OutlineItem } from '../src/editor/state/outline';
import { markdownSupport } from '../src/editor/syntax';
import { sampleDoc } from '../src/dev/sampleDocs';
import { allExamples, label } from './spec';
import { rng } from './helpers';

/** Lezer（同一套语法扩展）解析出的顶层标题：Document 的直接子节点里的 ATX / setext 标题 */
function lezerHeadings(doc: string): string[] {
  const state = EditorState.create({ doc, extensions: [markdownSupport()] });
  const tree = ensureSyntaxTree(state, doc.length, 1e9)!;
  const out: string[] = [];
  const c = tree.cursor();
  if (c.firstChild()) do {
    const m = /^(?:ATXHeading|SetextHeading)(\d)$/.exec(c.name);
    if (m) out.push(`h${m[1]}@${c.from}`);
  } while (c.nextSibling());
  return out;
}
const scannerHeadings = (doc: string) => scanOutline(Text.of(doc.split('\n'))).headings.map(h => `h${h.level}@${h.from}`);

const repoDocs = (() => {
  const dir = resolve(import.meta.dirname, '../../../docs');
  try {
    return readdirSync(dir).filter(f => f.endsWith('.md')).map(f => ({ name: `docs/${f}`, text: readFileSync(resolve(dir, f), 'utf8').replace(/\r\n/g, '\n') }));
  } catch { return []; }
})();
const specDoc = allExamples.map(e => e.markdown).join('\n\n');

describe('大纲扫描器', () => {
  it('示例文档与仓库 docs/：与 Lezer 解析出的顶层标题逐个相同', () => {
    const corpus = [
      ...['ime', 'small', 'mid', 'rich', 'large', 'large-rich'].map(n => ({ name: n, text: sampleDoc(n) })),
      ...repoDocs,
    ];
    for (const { name, text } of corpus) expect(scannerHeadings(text), name).toEqual(lezerHeadings(text));
  });

  it('CommonMark / GFM 用例：与 Lezer 的顶层标题只在「缩进 1–3 格的标题」上不同', () => {
    const diff = allExamples.filter(e => scannerHeadings(e.markdown).join() !== lezerHeadings(e.markdown).join());
    // 只认第 0 列起头的结构是有意的取舍（见 outline.ts 文件头）：缩进的行分不清是顶层还是列表续行，一律不进大纲。
    // 以下用例都是缩进 1–3 格的 ATX / setext 标题；清单有变化时要看一眼差异是不是新的一类
    expect(diff.map(label).map(l => l.split(' ').slice(0, 2).join(' '))).toEqual([
      'commonmark #68', 'commonmark #71', 'commonmark #82', 'commonmark #84',
      'gfm #38', 'gfm #41', 'gfm #52', 'gfm #54',
    ]);
  });

  describe('随机编辑后增量结果 ≡ 全量扫描', () => {
    const pieces = ['\n', '\n\n', '# ', '## ', '#', '```', '```js\n', '~~~', '$$', '$$\n', '\n---\n', '---', '===', '- ', '1. ', '2) ', '> ', '    ', '  ',
      '<!--', '-->', '<div>', '</div>', '<pre>', '| a | b |\n|---|---|\n', '|---|', 'x', '中', '\t', '...'];
    const seeds: [string, string, number][] = [
      ['示例文档', sampleDoc('rich').slice(0, 30000), 1],
      ['示例文档（front matter 起头）', '---\ntitle: t\n---\n' + sampleDoc('rich').slice(0, 30000), 2],
      ['CommonMark+GFM 用例合集', specDoc, 3],
      ['CommonMark+GFM 用例合集', specDoc, 4],
    ];
    for (const [name, doc, seed] of seeds) {
      it(`${name}（种子 ${seed}）`, () => {
        const random = rng(seed);
        let text = Text.of(doc.split('\n'));
        let outline = scanOutline(text);
        for (let i = 0; i < 400; i++) {
          // 一次事务 1–3 处改动：插入片段、删除、偶尔整段粘贴
          const len = text.length;
          const specs: { from: number; to: number; insert: string }[] = [];
          const n = 1 + Math.floor(random() * random() * 3);
          for (let k = 0; k < n; k++) {
            const from = Math.floor(random() * len);
            const to = random() < 0.3 ? Math.min(len, from + Math.floor(random() * 80)) : from;
            const insert = random() < 0.02 ? doc.slice(Math.floor(random() * doc.length)).slice(0, 3000) : random() < 0.85 ? pieces[Math.floor(random() * pieces.length)] : '';
            specs.push({ from, to, insert });
          }
          specs.sort((a, b) => a.from - b.from);
          for (let k = 1; k < specs.length; k++) if (specs[k].from < specs[k - 1].to) specs[k].from = specs[k].to = specs[k - 1].to;
          const changes = ChangeSet.of(specs, len);
          const next = changes.apply(text);
          outline = updateOutline(outline, text, next, changes);
          text = next;
          expect(outline.items, `第 ${i} 次编辑`).toEqual(scanOutline(text).items);
        }
      });
    }
  });

  it('段落里打字不改 revision；改标题文本、级别或增删标题才改', () => {
    let s = EditorState.create({ doc: '# 一\n\n正文\n\n## 二\n\n```\n# 代码\n```\n', extensions: [outlineField] });
    const rev = () => s.field(outlineField).revision;
    const r0 = rev();
    s = s.update({ changes: { from: 7, insert: '字' } }).state;
    expect(rev()).toBe(r0);
    s = s.update({ changes: { from: 22, insert: 'x' } }).state; // 围栏代码里
    expect(rev()).toBe(r0);
    s = s.update({ changes: { from: 3, insert: '改' } }).state;
    expect(rev()).toBe(r0 + 1);
    s = s.update({ changes: { from: 0, insert: '#' } }).state;
    expect(rev()).toBe(r0 + 2);
    expect(s.field(outlineField).headings.map(h => [h.level, h.text])).toEqual([[2, '一改'], [2, '二']]);
  });

  it('标题 id：去掉行内标记后转 slug，重名依次编号；当前标题按位置查找', () => {
    const o = scanOutline(Text.of('# Hello **World**\n\n# Hello World\n\n## 中文 标题！\n\n# `code` & [link](u)\n\n#\n'.split('\n')));
    expect(headingIds(o.headings)).toEqual(['hello-world', 'hello-world-1', '中文-标题', 'code-link', 'section']);
    expect(headingIndexAt(o, 0)).toBe(0);
    expect(headingIndexAt(o, o.headings[2].from + 3)).toBe(2);
    const before: OutlineItem[] = [];
    expect(headingIndexAt({ items: before, headings: before, revision: 0 }, 5)).toBe(-1);
  });
});
