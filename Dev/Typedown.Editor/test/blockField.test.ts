import { describe, expect, it } from 'vitest';
import { EditorSelection, EditorState } from '@codemirror/state';
import { ensureSyntaxTree } from '@codemirror/language';
import { blockField, blockFieldStats, createBlockState } from '../src/editor/widgets/blockField';
import { revealFrozen, setRevealFrozen, refreshReveal } from '../src/editor/decorations/revealState';
import { DiagramWidget } from '../src/editor/widgets/blockWidgets';
import { TableWidget } from '../src/editor/widgets/tableWidget';
import { markdownSupport } from '../src/editor/syntax';
import { sampleDoc } from '../src/dev/sampleDocs';
import { rng } from './helpers';

const extensions = [markdownSupport(), revealFrozen, blockField];

function create(doc: string, cursor = 0) {
  let state = EditorState.create({ doc, extensions, selection: { anchor: cursor } });
  ensureSyntaxTree(state, doc.length, 1e9);
  state = state.update({}).state;
  return state;
}

/** 块装饰翻成可比较的描述 */
function describeDecos(state: EditorState, decos = state.field(blockField).decos) {
  const out: string[] = [];
  decos.between(0, state.doc.length, (from, to, d) => {
    const w = d.spec.widget;
    const kind = w instanceof TableWidget ? 'table' : w instanceof DiagramWidget ? `${w.kind}${w.preview ? '-preview' : ''}` : '?';
    const src = w instanceof TableWidget ? w.src : w instanceof DiagramWidget ? w.src : '';
    out.push(`${kind}@${from}-${to}:${src}`);
  });
  return out;
}

const doc = '# 标题\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n$$\nx^2\n$$\n\n```mermaid\ngraph TD\nA-->B\n```\n\n```js\ncode\n```\n\n段落';

describe('块组件 StateField', () => {
  it('识别顶层表格、公式块、mermaid；普通围栏代码不是块组件', () => {
    expect(describeDecos(create(doc))).toEqual([
      'table@6-35:| a | b |\n|---|---|\n| 1 | 2 |',
      'math@37-46:x^2',
      'mermaid@48-77:graph TD\nA-->B',
    ]);
  });
  it('光标进入公式块：源码显示、块后是预览；离开后回到渲染态', () => {
    let s = create(doc);
    s = s.update({ selection: { anchor: 40 } }).state;
    expect(describeDecos(s)).toContain('math-preview@46-46:x^2');
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
    const pieces = ['|', ' | ', '\n', '\n\n', '$$', '$$\n', '```', '```mermaid\n', '-', 'x', '中', '#', '---', '> '];
    let s = create(sampleDoc('rich').slice(0, 12000) + doc);
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
  });

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
