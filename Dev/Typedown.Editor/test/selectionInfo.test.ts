import { describe, expect, it } from 'vitest';
import { Text } from '@codemirror/state';
import { blockContextAt, imageAt, inlineMarksAt } from '../src/editor/state/selectionInfo';
import { countStats } from '../src/editor/state/stats';
import { selectionPayload } from '../src/bridge/reporters';
import { stateOf } from './helpers';

/** 文本里 `‸` 标出光标（一个）或选区（两个），返回去掉标记的状态与位置 */
function at(src: string) {
  const a = src.indexOf('‸'), b = src.indexOf('‸', a + 1);
  const doc = src.replace(/‸/g, '');
  const state = stateOf(doc);
  return b < 0 ? { state, from: a, to: a } : { state, from: a, to: b - 1 };
}
const ctx = (src: string) => { const { state, from, to } = at(src); return blockContextAt(state, from, to); };
const marks = (src: string) => { const { state, from, to } = at(src); return inlineMarksAt(state, from, to); };

describe('BlockContext', () => {
  it('段落、标题、空行', () => {
    expect(ctx('ab‸c\n')).toEqual({ kinds: ['paragraph'], multipleBlocks: false, codeLike: false, codeLine: false, blockCommandsDisabled: false });
    expect(ctx('## ti‸tle\n').kinds).toEqual(['heading2']);
    expect(ctx('## title‸\n\nx').kinds).toEqual(['heading2']);
    expect(ctx('Title‸\n===\n').kinds).toEqual(['heading1']);
    expect(ctx('a\n\n‸\n\nb').kinds).toEqual(['paragraph']);
  });

  it('列表只看最近一层；任务列表；引用；多种同时命中时去掉段落', () => {
    expect(ctx('- a‸b\n').kinds).toEqual(['bulletList']);
    expect(ctx('1. a‸b\n').kinds).toEqual(['orderedList']);
    expect(ctx('- [ ] ta‸sk\n').kinds).toEqual(['taskList']);
    expect(ctx('> - a‸b\n').kinds).toEqual(['quote', 'bulletList']);
    expect(ctx('1. x\n   - a‸b\n').kinds).toEqual(['bulletList']);
  });

  it('代码类块：围栏代码、mermaid、公式、HTML；定界行不算代码行', () => {
    expect(ctx('```js\nco‸de\n```\n')).toMatchObject({ kinds: ['codeBlock'], codeLike: true, codeLine: true });
    expect(ctx('```j‸s\ncode\n```\n')).toMatchObject({ kinds: ['codeBlock'], codeLike: true, codeLine: false });
    expect(ctx('```mermaid\ngra‸ph TD\n```\n')).toMatchObject({ kinds: ['mermaidDiagram'], codeLike: true });
    expect(ctx('$$\nx‸^2\n$$\n')).toMatchObject({ kinds: ['mathBlock'], codeLike: true, codeLine: true });
    expect(ctx('<div>\nh‸i\n</div>\n')).toMatchObject({ kinds: ['htmlBlock'], codeLike: true });
  });

  it('新增的块结构：引用里的任务、任务里的普通子项、列表续行、引用里的空行', () => {
    expect(ctx('> - [x] 任‸务\n').kinds).toEqual(['quote', 'taskList']);
    expect(ctx('- [ ] 任务\n  - 子‸项\n').kinds).toEqual(['bulletList']);
    expect(ctx('- 第一行\n  续‸行\n').kinds).toEqual(['bulletList']);
    expect(ctx('1. a\n\n   第二‸段\n').kinds).toEqual(['orderedList']);
    expect(ctx('> a\n>‸\n> b\n').kinds).toEqual(['quote']);
  });

  it('表格里块命令禁用', () => {
    expect(ctx('| a | b |\n| - | - |\n| 1 | ‸2 |\n')).toMatchObject({ kinds: ['table'], blockCommandsDisabled: true });
  });

  it('选区跨块', () => {
    expect(ctx('a‸b\n\nc‸d\n').multipleBlocks).toBe(true);
    expect(ctx('a‸b\ncd‸\n').multipleBlocks).toBe(false);
  });
});

describe('行内标记', () => {
  it('光标严格在标记内部才算，紧贴外侧不算', () => {
    expect(marks('x **a‸b** y')).toEqual(['strong']);
    expect(marks('x ‸**ab** y')).toEqual([]);
    expect(marks('x **ab**‸ y')).toEqual([]);
    expect(marks('***a‸b***')).toEqual(['strong', 'emphasis']);
    expect(marks('~~a‸b~~ ==c== `d` $e$')).toEqual(['strikethrough']);
    expect(marks('==c‸d==')).toEqual(['highlight']);
    expect(marks('`c‸d`')).toEqual(['inlineCode']);
    expect(marks('$c‸d$')).toEqual(['inlineMath']);
    expect(marks('[te‸xt](u)')).toEqual(['link']);
    expect(marks('![al‸t](u)')).toEqual(['image']);
  });

  it('下划线 <u>…</u>：标签配对后按同样的规则判断，嵌在强调里也认', () => {
    expect(marks('a<u>下划‸线</u>b')).toEqual(['underline']);
    expect(marks('a‸<u>下划线</u>b')).toEqual([]);
    expect(marks('a<u>下划线</u>‸b')).toEqual([]);
    expect(marks('**<u>粗‸下</u>**')).toEqual(['strong', 'underline']);
    expect(marks('<u>‸ab‸</u>')).toEqual(['underline']);
    expect(marks('<u>a‸b</u> c‸')).toEqual([]);
    expect(marks('<u>未闭合‸')).toEqual([]);
    expect(marks('<b>粗‸</b>')).toEqual([]);
  });

  it('选区整段落在标记里才算', () => {
    expect(marks('**‸ab‸**')).toEqual(['strong']);
    expect(marks('**a‸b** c‸')).toEqual([]);
  });
});

describe('光标所在的图片', () => {
  const img = (src: string) => { const { state, from, to } = at(src); return imageAt(state, from, to); };
  it('选区落在图片源码内（含两端）时给出 src、alt、title', () => {
    expect(img('前‸![图](a.png "题")后')).toEqual({ src: 'a.png', alt: '图', title: '题' });
    expect(img('前![图‸](a.png)后')).toEqual({ src: 'a.png', alt: '图', title: '' });
    expect(img('前![图](a.png)‸后')).toEqual({ src: 'a.png', alt: '图', title: '' });
    expect(img('前<img src="b.png" alt="x"‸>后')).toEqual({ src: 'b.png', alt: 'x', title: '' });
  });
  it('不在图片里、引用式图片、选区越出图片时为 null', () => {
    expect(img('‸前![图](a.png)')).toBeNull();
    expect(img('![a][r‸]\n\n[r]: x')).toBeNull();
    expect(img('‸前![图](a.png)‸')).toBeNull();
  });
  it('selection.changed 的 rich 带上它', () => {
    const { state } = at('![图](a.png)‸');
    const s = state.update({ selection: { anchor: 3 } }).state;
    expect(selectionPayload(s, false).rich?.selectedImage).toEqual({ src: 'a.png', alt: '图', title: '' });
  });
});

describe('selection.changed 载荷', () => {
  it('text 只给单行且不超过 200 码元的选区；源码模式 rich 为 null', () => {
    const s = stateOf('hello world\nsecond');
    const sel = (anchor: number, head: number) => s.update({ selection: { anchor, head } }).state;
    expect(selectionPayload(sel(6, 11), false)).toMatchObject({ hasText: true, text: 'world', anchor: 6, head: 11 });
    expect(selectionPayload(sel(11, 6), false)).toMatchObject({ text: 'world', anchor: 11, head: 6 });
    expect(selectionPayload(sel(6, 14), false).text).toBe('');
    expect(selectionPayload(sel(3, 3), true)).toEqual({ hasText: false, text: '', rich: null, anchor: 3, head: 3 });
    const long = stateOf('a'.repeat(300)).update({ selection: { anchor: 0, head: 201 } }).state;
    expect(selectionPayload(long, false)).toMatchObject({ hasText: true, text: '' });
    const exact = stateOf('a'.repeat(300)).update({ selection: { anchor: 0, head: 200 } }).state;
    expect(selectionPayload(exact, false).text.length).toBe(200);
  });
});

describe('字数统计', () => {
  it('与 Muya wordCount 口径相同', () => {
    const muya = (md: string) => {
      const removed = md.replace(/[一-龥]/g, '');
      const tokens = removed.split(/[\s\n]+/).filter(t => t);
      const cjk = md.length - removed.length;
      return { characters: tokens.reduce((a, t) => a + t.length, 0) + cjk, words: cjk + tokens.length };
    };
    for (const md of ['', 'hello world', '中文 混排 abc中def', '# 标题\n\n- a\n- b\t c　d\n', 'x\n\n\ny  ']) {
      expect(countStats(Text.of(md.split('\n')))).toEqual(muya(md));
    }
  });
});
