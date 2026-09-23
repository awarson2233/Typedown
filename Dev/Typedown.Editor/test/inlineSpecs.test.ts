import { describe, expect, it } from 'vitest';
import { buildInlineSpecs, SYNTAX, type InlineSpec } from '../src/editor/decorations/inlineSpecs';
import { fullTree, stateOf } from './helpers';

/** 在 doc 上以 reveal 位置求显形描述，并把结果翻成便于断言的字符串 */
function specs(doc: string, reveal: number[]) {
  const s = stateOf(doc);
  const list = buildInlineSpecs(s.doc, fullTree(s), { from: 0, to: s.doc.length }, reveal);
  const text = (x: InlineSpec) => ('from' in x ? s.sliceDoc(x.from, x.to) : '');
  return {
    list,
    hidden: list.filter(x => x.kind === 'hide').map(text),
    gray: list.filter(x => x.kind === 'mark' && x.cls === SYNTAX).map(text),
    widgets: list.filter(x => x.kind === 'widget').map(x => `${(x as { widget: { type: string } }).widget.type}:${text(x)}`),
    lines: list.filter(x => x.kind === 'line').map(x => (x as { at: number; cls: string; quoteDepth: number })).map(l => `${s.doc.lineAt(l.at).number}:${l.cls}${l.quoteDepth ? '/' + l.quoteDepth : ''}`),
    marks: (cls: string) => list.filter(x => x.kind === 'mark' && x.cls === cls).map(text),
  };
}

const doc = '前文**粗体**后文';
// 偏移：前0 文1 *2 *3 粗4 体5 *6 *7 后8 文9
describe('行内 span 显形', () => {
  it('光标不在 span 内：标记隐藏，样式照常', () => {
    const r = specs(doc, [0]);
    expect(r.hidden).toEqual(['**', '**']);
    expect(r.marks('cm-td-strong')).toEqual(['**粗体**']);
  });
  it('光标落在 [from, to] 内（含两端）：标记变灰', () => {
    for (const p of [2, 5, 8]) {
      const r = specs(doc, [p]);
      expect(r.hidden).toEqual([]);
      expect(r.gray).toEqual(['**', '**']);
    }
  });
  it('只显形光标所在的那个 span', () => {
    const r = specs('*a* 与 `b` 与 ~~c~~ 与 ==d==', [1]);
    expect(r.gray).toEqual(['*', '*']);
    expect(r.hidden).toEqual(['`', '`', '~~', '~~', '==', '==']);
  });
  it('嵌套：外层显形不影响内层隐藏的判断', () => {
    const r = specs('**外 *内* 外**', [1]);
    expect(r.gray).toEqual(['**', '**']);
    expect(r.hidden).toEqual(['*', '*']);
  });
  it('链接：不显形时隐藏 [ 与 ](url)，显形时 URL 变灰', () => {
    const d = '看[文字](https://e.com)吧';
    expect(specs(d, [0]).hidden).toEqual(['[', '](https://e.com)']);
    expect(specs(d, [0]).marks('cm-td-link')).toEqual(['文字']);
    const shown = specs(d, [3]);
    expect(shown.hidden).toEqual([]);
    expect(shown.list.some(x => x.kind === 'mark' && x.cls.includes('cm-td-url'))).toBe(true);
  });
  it('跨行的链接目标不能用替换装饰隐藏，退为变灰', () => {
    const r = specs('[a](\n/url\n)', [99]);
    expect(r.hidden).toEqual(['[']);
    expect(r.gray).toEqual(['](\n/url\n)']);
  });
  it('行内公式：不显形时换成渲染 widget，显形时显示源码', () => {
    expect(specs('x $a^2$ y', [0]).widgets).toEqual(['inline-math:$a^2$']);
    expect(specs('x $a^2$ y', [3]).widgets).toEqual([]);
    expect(specs('x $a^2$ y', [3]).gray).toEqual(['$', '$']);
  });
  it('转义字符：不显形时只隐藏反斜杠', () => {
    expect(specs('a \\* b', [0]).hidden).toEqual(['\\']);
  });
});

describe('块级标记', () => {
  it('ATX 标题：光标在标题行内 # 变灰，否则连同空格隐藏；字号类名两种状态都有', () => {
    const d = '## 标题\n\n正文';
    const off = specs(d, [d.length]);
    const on = specs(d, [4]);
    expect(off.hidden).toEqual(['## ']);
    expect(on.gray).toEqual(['##']);
    expect(off.lines).toEqual(on.lines);
    expect(on.lines).toEqual(['1:cm-td-h cm-td-h2']);
  });
  it('列表符号、任务框始终换成 widget，与光标无关', () => {
    const d = '- 一\n  - 二\n1. 三\n- [x] 四';
    for (const p of [0, 3, d.length]) {
      expect(specs(d, [p]).widgets).toEqual(['bullet:- ', 'bullet:- ', 'ordered:1. ', 'task:- [x] ']);
    }
  });
  it('引用：> 始终隐藏，按行施加竖线类名与嵌套深度', () => {
    const r = specs('> 外\n> > 内', [0]);
    expect(r.hidden).toEqual(['> ', '> ', '> ']);
    expect(r.lines).toEqual(['1:cm-td-quote/1', '2:cm-td-quote/2']);
  });
  it('围栏代码：按行施加代码块类名，围栏标记变灰', () => {
    const r = specs('```js\nx\n```', [99]);
    expect(r.lines).toEqual(['1:cm-td-code cm-td-code-first', '2:cm-td-code', '3:cm-td-code cm-td-code-last']);
    expect(r.gray).toEqual(['```', 'js', '```']);
  });
  it('分隔线：光标在行内显示源码，否则隐藏；行类名不变', () => {
    expect(specs('a\n\n---\n\nb', [0]).hidden).toEqual(['---']);
    expect(specs('a\n\n---\n\nb', [4]).gray).toEqual(['---']);
  });
  it('只处理给定区间（视口）内的节点', () => {
    const s = stateOf('**a**\n\n**b**');
    const list = buildInlineSpecs(s.doc, fullTree(s), { from: 7, to: 12 }, []);
    expect(list.filter(x => x.kind === 'hide').map(x => (x as { from: number }).from)).toEqual([7, 10]);
  });
});
