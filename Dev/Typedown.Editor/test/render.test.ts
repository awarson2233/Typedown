// @vitest-environment jsdom
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { ensureSyntaxTree, syntaxTree } from '@codemirror/language';
import { createEditor, type TypedownEditor } from '../src/editor/createEditor';
import { loadKatex } from '../src/renderers/katex';
import { loadPurify } from '../src/renderers/html';
import { commonmarkExamples, gfmExamples, type SpecExample } from './spec';
import { SPEC_BASE_PATH, expectedTree } from './renderExpected';
import { actualTree } from './renderActual';
import { firstDifference, show, type Block, type Difference } from './renderTree';

/**
 * 规范用例的渲染对照（webview-wysiwyg-engine.md 第 9 节 B0）：CommonMark 0.31.2 与 GFM 0.29 的每个用例，
 * 期望 HTML 抽成语义树（renderExpected.ts），编辑器装载用例后渲染态的 contentDOM 抽成同样的树（renderActual.ts），
 * 归一化后逐节点比对（renderTree.ts）。
 *
 * 渲染态：光标放在文末追加的哨兵段落上（与 Tools/style-parity 相同），被测内容都不处于显形态。用例的结尾会吞掉
 * 追加内容时（未闭合的代码块、HTML 块等），哨兵改放在文首。
 *
 * 白名单 fixtures/render-known.json：已知不通过的用例按原因分组，每组注明分类（Typora 式有意差异 / 尚未实现 / 待修缺陷）。
 * 断言：白名单外的用例全部通过；白名单里的每一条确实仍不通过（修好了就必须删掉，白名单只减不增）。
 * 设置环境变量 TD_RENDER_REPORT=<文件> 时把全部用例的结果（含第一处差异）写成 JSON，整理白名单时用。
 *
 * 视图跑在 jsdom 里：没有布局，但装饰、widget 的 toDOM 都真实运行；用例都很短，整篇都在视口内。
 */

export type Category = 'intentional' | 'unimplemented' | 'defect';
export type Severity = 'high' | 'medium' | 'low';

interface KnownGroup {
  category: Category;
  /** 待修缺陷的严重度 */
  severity?: Severity;
  /** 尚未实现的项归属的待办（第 9 节的编号或阶段） */
  todo?: string;
  reason: string;
  /** 用例标识：`commonmark #123`、`gfm #45` */
  cases: string[];
}

const knownPath = resolve(import.meta.dirname, 'fixtures/render-known.json');
const known: KnownGroup[] = JSON.parse(readFileSync(knownPath, 'utf8')).groups;
const knownById = new Map<string, KnownGroup>();
for (const g of known) for (const id of g.cases) knownById.set(id, g);

const SENTINEL = '哨兵段落 sentinel。';
const idOf = (e: SpecExample) => `${e.spec} #${e.number}`;

beforeAll(async () => {
  const rect = () => ({ left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0, toJSON() {} }) as DOMRect;
  const rects = () => Object.assign([], { item: () => null }) as unknown as DOMRectList;
  Range.prototype.getBoundingClientRect = rect;
  Range.prototype.getClientRects = rects;
  Element.prototype.getClientRects = rects;
  (globalThis as { ResizeObserver?: unknown }).ResizeObserver ??= class { observe() {} unobserve() {} disconnect() {} };
  // 公式与 HTML 块的渲染库按需加载；先载好，渲染结果与用例顺序无关
  await Promise.all([loadKatex(), loadPurify()]);
});

enum SentinelAt { End, Start }

/** 装载一篇：哨兵段落放在文末（被吞掉时放文首），光标在哨兵上 */
function mount(parent: HTMLElement, markdown: string): { ed: TypedownEditor; at: SentinelAt } {
  const ed = createEditor({ doc: '', parent });
  const load = (at: SentinelAt) => {
    const doc = at === SentinelAt.End ? markdown.replace(/\n*$/, '\n') + '\n' + SENTINEL + '\n' : SENTINEL + '\n\n' + markdown;
    const from = at === SentinelAt.End ? doc.length - 1 - SENTINEL.length : 0;
    ed.view.setState(ed.createState(doc, { anchor: from + SENTINEL.length }, { basePath: SPEC_BASE_PATH }));
    ensureSyntaxTree(ed.view.state, doc.length, 1e9);
    ed.view.dispatch({}); // 让语法树与块组件覆盖全文
    const top = syntaxTree(ed.view.state).resolve(from, 1);
    return top.name === 'Paragraph' && !!top.parent?.type.isTop && top.from === from && top.to === from + SENTINEL.length;
  };
  if (load(SentinelAt.End)) return { ed, at: SentinelAt.End };
  load(SentinelAt.Start);
  return { ed, at: SentinelAt.Start };
}

const isSentinel = (b: Block | undefined) => b?.t === 'paragraph' && b.c.length === 1 && b.c[0].t === 'text' && b.c[0].v === SENTINEL;

interface Outcome { id: string; section: string; markdown: string; diff: Difference | null }

function run(e: SpecExample, parent: HTMLElement): Outcome {
  const { ed, at } = mount(parent, e.markdown);
  try {
    const expected = expectedTree(e.html);
    const actual = actualTree(ed.view);
    const s = at === SentinelAt.End ? actual.pop() : actual.shift();
    if (!isSentinel(s)) return { id: idOf(e), section: e.section, markdown: e.markdown, diff: { path: '(sentinel)', expected: SENTINEL, actual: show(s) } };
    return { id: idOf(e), section: e.section, markdown: e.markdown, diff: firstDifference(expected, actual) };
  } finally {
    ed.view.destroy();
  }
}

const outcomes: Outcome[] = [];

afterAll(() => {
  const out = process.env.TD_RENDER_REPORT;
  if (!out) return;
  writeFileSync(out, JSON.stringify(outcomes.map(o => ({ ...o, known: knownById.get(o.id)?.reason ?? null })), null, 1));
});

function sections(examples: SpecExample[]): [string, SpecExample[]][] {
  const m = new Map<string, SpecExample[]>();
  for (const e of examples) {
    const key = e.section;
    m.set(key, [...(m.get(key) ?? []), e]);
  }
  return [...m];
}

const fmt = (o: Outcome) => `${o.id} ${o.section}: ${o.diff!.path}\n  期望 ${o.diff!.expected}\n  实际 ${o.diff!.actual}`;

for (const [spec, examples] of [['CommonMark 0.31.2', commonmarkExamples], ['GFM 0.29', gfmExamples]] as const) {
  describe(`渲染对照：${spec}`, () => {
    for (const [section, list] of sections(examples)) {
      it(`${section}（${list.length} 个用例）`, () => {
        const parent = document.createElement('div');
        document.body.appendChild(parent);
        const unexpected: string[] = [];
        const fixed: string[] = [];
        for (const e of list) {
          const o = run(e, parent);
          outcomes.push(o);
          const k = knownById.get(o.id);
          if (o.diff && !k) unexpected.push(fmt(o));
          if (!o.diff && k) fixed.push(`${o.id}（白名单：${k.reason}）已通过，请从 render-known.json 删除`);
        }
        parent.remove();
        expect(unexpected).toEqual([]);
        expect(fixed).toEqual([]);
      });
    }
  });
}

describe('渲染对照白名单', () => {
  it('每条都是存在的用例、只出现一次，分类与字段齐全', () => {
    const ids = new Set([...commonmarkExamples, ...gfmExamples].map(idOf));
    const seen = new Set<string>();
    const bad: string[] = [];
    for (const g of known) {
      if (!['intentional', 'unimplemented', 'defect'].includes(g.category)) bad.push(`分类不认识：${g.category}（${g.reason}）`);
      if (g.category === 'defect' && !['high', 'medium', 'low'].includes(g.severity ?? '')) bad.push(`缺陷没有严重度：${g.reason}`);
      if (g.category === 'unimplemented' && !g.todo) bad.push(`尚未实现的项没有注明归属：${g.reason}`);
      if (!g.cases.length) bad.push(`空分组：${g.reason}`);
      for (const id of g.cases) {
        if (!ids.has(id)) bad.push(`不存在的用例：${id}`);
        if (seen.has(id)) bad.push(`重复：${id}`);
        seen.add(id);
      }
    }
    expect(bad).toEqual([]);
  });
});
