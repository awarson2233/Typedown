import { describe, expect, it } from 'vitest';
import { EditorSelection, EditorState } from '@codemirror/state';
import { ensureSyntaxTree } from '@codemirror/language';
import type { Decoration } from '@codemirror/view';
import { blockField, blockFieldStats, createBlockState } from '../src/editor/widgets/blockField';
import { revealFrozen, setRevealFrozen, refreshReveal } from '../src/editor/decorations/revealState';
import { DiagramWidget, FenceRole, FenceWidget, FootnoteLabelWidget, HtmlWidget, TocWidget } from '../src/editor/widgets/blockWidgets';
import { TableWidget } from '../src/editor/widgets/tableWidget';
import { markdownSupport } from '../src/editor/syntax';
import { outlineField } from '../src/editor/state/outline';
import { footnoteField } from '../src/editor/state/footnotes';
import { sampleDoc } from '../src/dev/sampleDocs';
import { rng } from './helpers';

const extensions = [markdownSupport(), revealFrozen, outlineField, footnoteField, blockField];

function create(doc: string, cursor = 0) {
  let state = EditorState.create({ doc, extensions, selection: { anchor: Math.min(cursor, doc.length) } });
  ensureSyntaxTree(state, doc.length, 1e9);
  state = state.update({}).state;
  return state;
}

/** 一个块装饰翻成可比较的描述 */
function describeDeco(from: number, to: number, d: Decoration): string {
  const w = d.spec.widget;
  if (w instanceof TableWidget) return `table@${from}-${to}:${w.src}`;
  if (w instanceof DiagramWidget) return `${w.kind}${w.preview ? '-preview' : ''}@${from}-${to}:${w.src}`;
  if (w instanceof FenceWidget) return `fence-${w.role === FenceRole.Head ? 'head' : 'foot'}-${w.kind}${w.active ? '!' : ''}@${from}-${to}`;
  if (w instanceof HtmlWidget) return `html@${from}-${to}:${w.src}`;
  if (w instanceof TocWidget) return `toc@${from}-${to}:${w.entries.map(e => `${e.level}${e.text}`).join('|')}`;
  if (w instanceof FootnoteLabelWidget) return `fn-label@${from}-${to}:${w.label}=${w.number}`;
  if (w) return `${w.constructor.name}@${from}-${to}`;
  if (d.spec.class) return `${from === to ? 'line' : 'mark'}@${from}-${to}:${d.spec.class}`;
  return `?@${from}-${to}`;
}

function describeDecos(state: EditorState, decos = state.field(blockField).decos) {
  const out: string[] = [];
  decos.between(0, state.doc.length, (from, to, d) => { out.push(describeDeco(from, to, d)); });
  return out;
}

/** 只看块级 widget（表格、公式、mermaid、HTML、目录） */
const widgetsOnly = (s: EditorState) => describeDecos(s).filter(d => /^(table|math|mermaid|html|toc)/.test(d));

const doc = '# 标题\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n$$\nx^2\n$$\n\n```mermaid\ngraph TD\nA-->B\n```\n\n```js\ncode\n```\n\n段落';

describe('块组件 StateField', () => {
  it('识别顶层表格、公式块、mermaid', () => {
    expect(widgetsOnly(create(doc))).toEqual([
      'table@6-35:| a | b |\n|---|---|\n| 1 | 2 |',
      'math@37-46:x^2',
      'mermaid@48-77:graph TD\nA-->B',
    ]);
  });
  it('光标进入公式块：围栏收起、源码行成框、块后是预览；离开后回到渲染态', () => {
    let s = create(doc);
    s = s.update({ selection: { anchor: 40 } }).state;
    const d = describeDecos(s);
    expect(d).toContain('math-preview@46-46:x^2');
    expect(d).toContain('fence-head-math!@37-39');
    expect(d).toContain('fence-foot-math!@44-46');
    expect(d).toContain('line@40-40:cm-td-fence-line cm-td-fence-body cm-td-fence-math cm-td-fence-active');
    s = s.update({ selection: { anchor: 0 } }).state;
    expect(describeDecos(s)).toContain('math@37-46:x^2');
  });
  it('鼠标冻结期间不随选区切换两态，解冻后补上', () => {
    let s = create(doc);
    s = s.update({ effects: setRevealFrozen.of(true) }).state;
    s = s.update({ selection: { anchor: 40 } }).state;
    expect(describeDecos(s)).toContain('math@37-46:x^2');
    s = s.update({ effects: setRevealFrozen.of(false) }).state;
    expect(describeDecos(s)).toContain('math-preview@46-46:x^2');
  });
  it('组字事务只映射，不重扫', () => {
    let s = create(doc, 40);
    const before = blockFieldStats.updates;
    s = s.update({ changes: { from: 40, insert: '拼' }, userEvent: 'input.type.compose' }).state;
    expect(blockFieldStats.updates).toBe(before);
    expect(describeDecos(s)).toContain('math-preview@47-47:x^2');
    s = s.update({ effects: refreshReveal.of(null) }).state;
    expect(describeDecos(s)).toContain('math-preview@47-47:拼x^2');
  });

  it('随机编辑 300 次：覆盖范围内的增量结果 ≡ 全文解析后的全量扫描；解析追上后整篇相同', () => {
    const random = rng(42);
    const pieces = ['|', ' | ', '\n', '\n\n', '$$', '$$\n', '```', '```mermaid\n', '```js\n', '-', 'x', '中', '#', '## 新标题\n', '---', '> ',
      '<div>', '</div>\n', '<!-- c -->', '[TOC]', '\n[TOC]\n', '[^1]', '[^n]: ', '\n[^1]: 注\n', '![](a.png)'];
    let s = create('---\ntitle: t\n---\n\n' + sampleDoc('rich').slice(0, 12000) + doc + '\n\n[TOC]\n\n<div>\n<b>x</b>\n</div>\n\n正文[^1]\n\n[^1]: 脚注\n');
    for (let i = 0; i < 300; i++) {
      const len = s.doc.length;
      const from = Math.floor(random() * len);
      const del = random() < 0.3 ? Math.min(len - from, Math.floor(random() * 12)) : 0;
      const insert = random() < 0.8 ? pieces[Math.floor(random() * pieces.length)] : '';
      const tr = s.update({
        changes: { from, to: from + del, insert },
        selection: EditorSelection.cursor(Math.min(Math.floor(random() * (len + insert.length - del)), len + insert.length - del)),
        userEvent: 'input.type',
      });
      s = tr.state;
      const oracle = create(s.doc.toString(), s.selection.main.head);
      const covered = s.field(blockField).covered;
      const within = (d: string) => Number(d.split('@')[1].split('-')[1].split(':')[0]) <= covered;
      expect(describeDecos(s).filter(within), `第 ${i} 次编辑`).toEqual(describeDecos(oracle).filter(within));
      if (i % 10 === 9) {
        // 模拟后台解析追到文末
        ensureSyntaxTree(s, s.doc.length, 1e9);
        s = s.update({}).state;
        expect(s.field(blockField).covered, `第 ${i} 次编辑后追平`).toBe(s.doc.length);
        expect(describeDecos(s), `第 ${i} 次编辑后追平`).toEqual(describeDecos(oracle));
      }
    }
  }, 60000); // 每次编辑都全量解析一遍作对照，满载并行时会超过默认的 5 s

  it('段落内打字只重扫所在的块，与文档长度无关', () => {
    const big = sampleDoc('rich').repeat(5);
    let s = create(big);
    const at = big.indexOf('times.', big.length >> 1) + 6;
    s = s.update({ selection: { anchor: at } }).state;
    s = s.update({ changes: { from: at, insert: 'a' }, selection: { anchor: at + 1 }, userEvent: 'input.type' }).state;
    expect(blockFieldStats.lastRescan).toBeLessThan(400);
  });

  it('语法树未覆盖全文时只扫已解析部分，解析推进后补齐后段组件', () => {
    const big = sampleDoc('rich').repeat(6); // 约 24 万字符，创建时只同步解析开头一段
    let s = EditorState.create({ doc: big, extensions });
    const early = describeDecos(s).length;
    expect(s.field(blockField).covered).toBeLessThan(big.length);
    ensureSyntaxTree(s, big.length, 1e9);
    s = s.update({}).state;
    const all = describeDecos(s).length;
    expect(s.field(blockField).covered).toBe(big.length);
    expect(all).toBeGreaterThan(early);
    expect(all).toBe(describeDecos(s, createBlockState(s).decos).length);
  });
});

describe('围栏代码块', () => {
  const code = '段落\n\n```js\nlet a = 1;\nlet b = 2;\n```\n\n尾';
  it('首行只露语言名（``` 替换掉），内容行成框带语言类，闭围栏收起', () => {
    const d = describeDecos(create(code));
    expect(d).toEqual([
      'line@4-4:cm-td-fence-line cm-td-fence-lang-line cm-td-fence-code',
      'FencePrefixWidget@4-7',
      'mark@7-9:cm-td-code-lang',
      'line@10-10:cm-td-fence-line cm-td-fence-body cm-td-fence-code language-javascript',
      'line@21-21:cm-td-fence-line cm-td-fence-body cm-td-fence-code language-javascript',
      'fence-foot-code@32-35',
    ]);
  });
  it('光标进入：同样的结构，只加 active 类；没写语言时显示占位', () => {
    const d = describeDecos(create(code, 12));
    expect(d[0]).toBe('line@4-4:cm-td-fence-line cm-td-fence-lang-line cm-td-fence-code cm-td-fence-active');
    expect(d).toContain('fence-foot-code!@32-35');
    expect(describeDecos(create('```\nx\n```', 5))).toContain('LanguagePlaceholderWidget@3-3');
  });
  it('没有内容行的代码块、列表里的代码块按源码显示', () => {
    expect(describeDecos(create('```js\n```'))).toEqual([]);
    expect(describeDecos(create('- a\n\n  ```js\n  x\n  ```'))).toEqual([]);
  });
  it('未闭合的代码块延续到文末，块尾补一个收尾条', () => {
    expect(describeDecos(create('```py\nx = 1'))).toContain('fence-foot-code!@11-11');
  });
  it('mermaid 编辑态按代码块显示（语言行可改）并带预览', () => {
    const s = create('```mermaid\ngraph TD\n```', 12);
    expect(describeDecos(s)).toEqual([
      'line@0-0:cm-td-fence-line cm-td-fence-lang-line cm-td-fence-mermaid cm-td-fence-active',
      'FencePrefixWidget@0-3',
      'mark@3-10:cm-td-code-lang',
      'line@11-11:cm-td-fence-line cm-td-fence-body cm-td-fence-mermaid cm-td-fence-active',
      'fence-foot-mermaid!@20-23',
      'mermaid-preview@23-23:graph TD',
    ]);
  });
});

describe('front matter、HTML 块、[TOC]、脚注定义', () => {
  it('front matter：两条围栏收起，内容行成框', () => {
    expect(describeDecos(create('---\na: 1\n---\n\n正文', 15))).toEqual([
      'fence-head-frontmatter@0-3',
      'line@4-4:cm-td-fence-line cm-td-fence-body cm-td-fence-frontmatter language-yaml',
      'fence-foot-frontmatter@9-12',
    ]);
  });
  it('HTML 块：渲染态整块替换；光标进入显示源码；只有 script / style / 注释的块始终是源码', () => {
    const html = '<div align="center">\n<b>粗</b>\n</div>\n\n尾';
    expect(describeDecos(create(html, html.length))).toEqual(['html@0-36:<div align="center">\n<b>粗</b>\n</div>']);
    expect(describeDecos(create(html, 3))).toEqual(['line@0-0:cm-td-html-src', 'line@21-21:cm-td-html-src', 'line@30-30:cm-td-html-src']);
    expect(describeDecos(create('<script>\nalert(1)\n</script>\n\n尾', 30))).toEqual(['line@0-0:cm-td-html-src', 'line@9-9:cm-td-html-src', 'line@18-18:cm-td-html-src']);
    expect(describeDecos(create('<style>\np{}\n</style>\n\n尾', 30))).toEqual(['line@0-0:cm-td-html-src', 'line@8-8:cm-td-html-src', 'line@12-12:cm-td-html-src']);
    expect(describeDecos(create('<!-- 注释 -->\n\n尾', 30))).toEqual(['line@0-0:cm-td-html-src']);
  });
  it('[TOC]：渲染成目录（来自行首扫描的大纲）；标题变了目录跟着变；光标在行上显示源码', () => {
    let s = create('[TOC]\n\n# 一\n\n## 二 **粗**\n', 20);
    expect(describeDecos(s)).toEqual(['toc@0-5:1一|2二 粗']);
    s = s.update({ changes: { from: 10, insert: '甲' } }).state;
    expect(describeDecos(s)).toEqual(['toc@0-5:1一甲|2二 粗']);
    s = s.update({ selection: { anchor: 2 } }).state;
    expect(describeDecos(s)).toEqual(['line@0-0:cm-td-toc-src']);
  });
  it('脚注定义：按行成框，标签换成与引用一致的编号，光标进入标签时显形', () => {
    const md = '甲[^b]乙[^a]\n\n[^a]: 注 A\n续\n\n[^b]: 注 B\n';
    const s = create(md, 0);
    const d = describeDecos(s);
    expect(d).toContain('fn-label@12-18:a=2');
    expect(d).toContain('fn-label@25-31:b=1');
    expect(d).toContain('line@12-12:cm-td-footnote cm-td-footnote-first');
    expect(d).toContain('line@22-22:cm-td-footnote cm-td-footnote-last');
    expect(d).toContain('FootnoteBackWidget@23-23');
    expect(describeDecos(create(md, 14))).toContain('mark@12-18:cm-td-footnote-src');
    // 引用顺序变化后编号跟着变
    const s2 = s.update({ changes: { from: 0, insert: '[^a]' } }).state;
    expect(describeDecos(s2)).toContain('fn-label@16-22:a=1');
  });
});
