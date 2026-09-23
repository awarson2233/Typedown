import { describe, expect, it } from 'vitest';
import { EditorState, type Transaction } from '@codemirror/state';
import { ensureSyntaxTree } from '@codemirror/language';
import { history, redo, undo } from '@codemirror/commands';
import { markdownSupport } from '../src/editor/syntax';
import { sampleDoc } from '../src/dev/sampleDocs';
import { allExamples } from './spec';
import { rng } from './helpers';

/**
 * patches/ 下两份补丁的行为：
 * - @lezer/markdown：复用旧片段时整块拿走平衡树的匿名分组，不再逐个顶层块重建；
 * - @lezer/common：只被位移的片段保留 openStart/openEnd，未解析的旧改动边界不会在下一次编辑后丢失。
 * 核心性质：任意编辑、粘贴、撤销/重做序列之后，把解析追到文末得到的语法树与从零解析逐节点相同。
 */

const extensions = [markdownSupport(), history({ minDepth: 1000 })];
const fresh = (doc: string) => {
  const s = EditorState.create({ doc, extensions: [markdownSupport()] });
  return ensureSyntaxTree(s, doc.length, 1e9)!.toString();
};
const catchUp = (s: EditorState) => ensureSyntaxTree(s, s.doc.length, 1e9)!.toString();
const parsed = (doc: string) => {
  const s = EditorState.create({ doc, extensions });
  ensureSyntaxTree(s, doc.length, 1e9);
  return s.update({}).state;
};
const run = (cmd: typeof undo, s: EditorState) => {
  let out: Transaction | null = null;
  cmd({ state: s, dispatch: t => { out = t; } });
  return out as Transaction | null;
};

const big = sampleDoc('rich').repeat(3); // 约 12 万字符
// CommonMark + GFM 全部用例拼成一篇：各种块结构的边界情况挨在一起，最容易暴露片段复用的错误
const specDoc = allExamples.map(e => e.markdown).join('\n\n');

describe('增量解析 ≡ 从零解析（两份补丁合起来）', () => {
  const pieces = ['\n', '\n\n', '```', '```mermaid\n', '$$\n', '# ', '- ', '1. ', '> ', '    ', '| a | b |\n|---|---|\n', '|---|', '**', '*', '`', '<div>', '[x]: /u', 'x', '中', '---\n', '==='];
  for (const [name, doc, seed] of [['示例文档', big, 7], ['示例文档', big, 11], ['CommonMark+GFM 用例合集', specDoc, 3], ['CommonMark+GFM 用例合集', specDoc, 5]] as const) {
    it(`${name}（种子 ${seed}）：随机编辑、大段粘贴、撤销/重做，不定期追平`, () => {
      const random = rng(seed);
      let s = parsed(doc);
      let checks = 0;
      for (let i = 0; i < 200; i++) {
        const r = random();
        if (r < 0.1) { const t = run(undo, s); if (t) s = t.state; }
        else if (r < 0.15) { const t = run(redo, s); if (t) s = t.state; }
        else {
          const len = s.doc.length;
          const from = Math.floor(random() * len);
          const del = random() < 0.3 ? Math.min(len - from, Math.floor(random() * 60)) : 0;
          const paste = random() < 0.03;
          const insert = paste ? doc.slice(Math.floor(random() * (doc.length - 30000)), undefined).slice(0, 5000 + Math.floor(random() * 25000)) : pieces[Math.floor(random() * pieces.length)];
          s = s.update({ changes: { from, to: from + del, insert }, userEvent: paste ? 'input.paste' : 'input.type' }).state;
        }
        // 不定期追平（模拟后台解析），其余时候让未解析的片段跨过多次编辑
        if (random() < 0.12) { expect(catchUp(s), `第 ${i} 步`).toBe(fresh(s.doc.toString())); checks++; }
      }
      expect(catchUp(s)).toBe(fresh(s.doc.toString()));
      // 全部撤销回到原文
      for (let t = run(undo, s); t; t = run(undo, s)) s = t.state;
      expect(s.doc.toString()).toBe(doc);
      expect(catchUp(s)).toBe(fresh(doc));
      expect(checks).toBeGreaterThan(10);
    }, 120000);
  }
});
