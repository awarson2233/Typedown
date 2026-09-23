import { describe, expect, it } from 'vitest';
import { Text } from '@codemirror/state';
import { commonmarkLanguage } from '@codemirror/lang-markdown';
import type { MarkdownConfig, MarkdownParser } from '@lezer/markdown';
import type { Tree } from '@lezer/common';
import { buildInlineSpecs, SYNTAX, GAP_LINE, type InlineSpec, type InlineSpecOptions } from '../src/editor/decorations/inlineSpecs';
import { markdownExtensions } from '../src/editor/syntax';
import { fullTree, stateOf } from './helpers';

/** 把显形描述翻成便于断言的字符串 */
function describeSpecs(doc: Text, tree: Tree, reveal: number[], options?: InlineSpecOptions) {
  const list = buildInlineSpecs(doc, tree, { from: 0, to: doc.length }, reveal, options);
  const text = (x: InlineSpec) => ('from' in x ? doc.sliceString(x.from, x.to) : '');
  const kind = <K extends InlineSpec['kind']>(k: K) => list.filter((x): x is Extract<InlineSpec, { kind: K }> => x.kind === k);
  return {
    list,
    hidden: kind('hide').map(text),
    gray: kind('mark').filter(x => x.cls === SYNTAX).map(text),
    widgets: kind('widget').map(x => `${x.widget.type}:${text(x)}`),
    widgetSpecs: kind('widget').map(x => x.widget),
    points: kind('point').map(x => ({ at: x.at, side: x.side, widget: x.widget })),
    elements: kind('element').map(x => `${x.tag}${x.attrs}:${text(x)}`),
    lines: kind('line').map(x => `${doc.lineAt(x.at).number}:${x.cls}`),
    styles: kind('line').filter(x => x.style).map(x => `${doc.lineAt(x.at).number}:${x.style}`),
    marks: (cls: string) => kind('mark').filter(x => x.cls === cls).map(text),
  };
}

/** 在 doc 上以 reveal 位置求显形描述 */
function specs(doc: string, reveal: number[]) {
  const s = stateOf(doc);
  return describeSpecs(s.doc, fullTree(s), reveal);
}

/** 行装饰里去掉块间空行，便于只看结构类名 */
const structural = (lines: string[]) => lines.filter(l => !l.endsWith(':' + GAP_LINE));

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
  it('行内代码：底色只包内容，反引号在外', () => {
    expect(specs('a `code` b', [0]).marks('cm-td-code-inline')).toEqual(['code']);
    expect(specs('a ``x ` y`` b', [3]).gray).toEqual(['``', '``']);
  });
  it('链接：不显形时隐藏 [ 与 ](url)，显形时 URL 以链接地址样式显示', () => {
    const d = '看[文字](https://e.com)吧';
    expect(specs(d, [0]).hidden).toEqual(['[', '](https://e.com)']);
    expect(specs(d, [0]).marks('cm-td-link')).toEqual(['文字']);
    const shown = specs(d, [3]);
    expect(shown.hidden).toEqual([]);
    expect(shown.marks('cm-td-url')).toEqual(['https://e.com']);
    expect(shown.gray).toEqual(['[', ']', '(', ')']);
  });
  it('跨行的链接目标不能用替换装饰隐藏，退为变灰', () => {
    const r = specs('[a](\n/url\n)', [99]);
    expect(r.hidden).toEqual(['[']);
    expect(r.gray).toEqual(['](\n/url\n)']);
  });
  it('行内公式：不显形时换成渲染 widget；显形时显示源码，并在源码前挂浮出预览', () => {
    expect(specs('x $a^2$ y', [0]).widgets).toEqual(['inline-math:$a^2$']);
    const shown = specs('x $a^2$ y', [3]);
    expect(shown.widgets).toEqual([]);
    expect(shown.gray).toEqual(['$', '$']);
    expect(shown.points).toEqual([{ at: 2, side: -1, widget: { type: 'inline-math', src: 'a^2' } }]);
  });
  it('转义字符：不显形时只隐藏反斜杠', () => {
    expect(specs('a \\* b', [0]).hidden).toEqual(['\\']);
  });
  it('自动链接：尖括号在不显形时隐藏；GFM 裸链接直接以链接样式显示', () => {
    expect(specs('见 <https://e.com> 吧', [0]).hidden).toEqual(['<', '>']);
    expect(specs('见 <https://e.com> 吧', [3]).gray).toEqual(['<', '>']);
    expect(specs('见 https://e.com 吧', [0]).marks('cm-td-link')).toEqual(['https://e.com']);
    expect(specs('见 www.e.com 吧', [0]).marks('cm-td-link')).toEqual(['www.e.com']);
  });
});

describe('图片', () => {
  const d = '前![图](a.png "标题")后';
  it('渲染态：整段换成图片 widget（地址是源码原文）', () => {
    const r = specs(d, [0]);
    expect(r.widgets).toEqual(['image:![图](a.png "标题")']);
    expect(r.widgetSpecs[0]).toEqual({ type: 'image', src: 'a.png', alt: '图', title: '标题' });
  });
  it('显形态（含两端）：保留源码，图片 widget 挂在源码之后', () => {
    for (const p of [1, 4, d.length - 1]) {
      const r = specs(d, [p]);
      expect(r.widgets).toEqual([]);
      expect(r.gray).toEqual(['![', ']', '(', '"标题"', ')']);
      expect(r.marks('cm-td-image-alt')).toEqual(['图']);
      expect(r.marks('cm-td-image-src')).toEqual(['a.png']);
      expect(r.points).toEqual([{ at: d.length - 1, side: 1, widget: { type: 'image', src: 'a.png', alt: '图', title: '标题' } }]);
    }
  });
  it('没有标题时 title 为 null；链接里的图片同样处理', () => {
    expect(specs('![](x.png)', [99]).widgetSpecs).toEqual([{ type: 'image', src: 'x.png', alt: '', title: null }]);
    expect(specs('[![a](x.png)](https://e.com)', [99]).widgets).toEqual(['image:![a](x.png)']);
  });
  it('引用式图片没有地址，按源码显示', () => {
    const r = specs('![a][ref]\n\n[ref]: x.png', [99]);
    expect(r.widgets).toEqual([]);
    expect(r.marks('cm-td-image-src')).toEqual(['![a][ref]']);
  });
});

describe('行内 HTML', () => {
  it('成对的白名单标签：内容包进真实元素；不显形时隐藏标签，显形时标签以源码显示', () => {
    const d = 'a<u>下划线</u>b';
    const off = specs(d, [0]);
    expect(off.elements).toEqual(['u:下划线']);
    expect(off.hidden).toEqual(['<u>', '</u>']);
    const on = specs(d, [3]);
    expect(on.elements).toEqual(['u:下划线']);
    expect(on.hidden).toEqual([]);
    expect(on.marks('cm-td-html-tag')).toEqual(['<u>', '</u>']);
  });
  it('属性原文交给视图层消毒；嵌套与强调里的标签按各自的父节点配对', () => {
    expect(specs('<span style="color:red">红</span>', [99]).elements).toEqual(['span style="color:red":红']);
    expect(specs('<b>粗<i>斜</i></b>', [99]).elements).toEqual(['b:粗<i>斜</i>', 'i:斜']);
    expect(specs('**<u>x</u>**', [99]).elements).toEqual(['u:x']);
  });
  it('不在白名单的标签与落单的标签始终以源码显示', () => {
    expect(specs('<script>x</script>', [99]).marks('cm-td-html-tag')).toEqual([]);
    const r = specs('a <iframe>b</iframe> <u>c d', [0]);
    expect(r.elements).toEqual([]);
    expect(r.marks('cm-td-html-tag')).toEqual(['<iframe>', '</iframe>', '<u>']);
  });
  it('<br> 渲染成换行，<img> 渲染成图片，注释不显形时隐藏', () => {
    expect(specs('a<br>b', [0]).widgets).toEqual(['html-break:<br>']);
    expect(specs('a<br>b', [2]).marks('cm-td-html-tag')).toEqual(['<br>']);
    const img = specs('a<img src="x.png" alt=\'图\'>b', [0]);
    expect(img.widgetSpecs).toEqual([{ type: 'image', src: 'x.png', alt: '图', title: null }]);
    const imgShown = specs('a<img src="x.png">b', [3]);
    expect(imgShown.points.map(p => p.widget.type)).toEqual(['image']);
    expect(specs('a<!-- 注 -->b', [0]).hidden).toEqual(['<!-- 注 -->']);
    expect(specs('a<!-- 注 -->b', [3]).marks('cm-td-html-tag')).toEqual(['<!-- 注 -->']);
  });
});

describe('emoji', () => {
  it('已知别名：不显形时换成字符，显形时冒号变灰、名字高亮', () => {
    expect(specs('好 :smile: 的', [0]).widgetSpecs).toEqual([{ type: 'emoji', char: '😄' }]);
    const on = specs('好 :smile: 的', [3]);
    expect(on.gray).toEqual([':', ':']);
    expect(on.marks('cm-td-emoji-name')).toEqual(['smile']);
  });
  it('未知别名保持纯文本（例如时间 12:30:45）', () => {
    const r = specs('12:30:45 与 :not_an_emoji_xx:', [0]);
    expect(r.widgets).toEqual([]);
    expect(r.gray).toEqual([]);
  });
});

describe('脚注引用', () => {
  // 脚注语法由块组件一侧提供；这里用一个只在测试里定义、产出同名节点的扩展验证显形规则
  const TestFootnote: MarkdownConfig = {
    defineNodes: ['FootnoteReference', 'FootnoteReferenceMark', 'FootnoteLabel'],
    parseInline: [{
      name: 'FootnoteReference',
      before: 'Link',
      parse(cx, next, pos) {
        if (next !== 91 || cx.char(pos + 1) !== 94) return -1;
        const m = /^\[\^([^\]\s]+)\]/.exec(cx.slice(pos, cx.end));
        if (!m) return -1;
        const end = pos + m[0].length;
        return cx.addElement(cx.elt('FootnoteReference', pos, end, [
          cx.elt('FootnoteReferenceMark', pos, pos + 2), cx.elt('FootnoteLabel', pos + 2, end - 1), cx.elt('FootnoteReferenceMark', end - 1, end),
        ]));
      },
    }],
  };
  const parser = (commonmarkLanguage.parser as MarkdownParser).configure([...markdownExtensions, TestFootnote]);
  const fn = (src: string, reveal: number[], options?: InlineSpecOptions) => describeSpecs(Text.of(src.split('\n')), parser.parse(src), reveal, options);

  it('不显形时换成上标：有编号用编号，没有编号用标签原文', () => {
    expect(fn('正文[^note]。', [0]).widgetSpecs).toEqual([{ type: 'footnote-ref', label: 'note', number: null }]);
    const numbers = new Map([['note', 3]]);
    expect(fn('正文[^note]。', [0], { footnoteNumber: l => numbers.get(l) }).widgetSpecs).toEqual([{ type: 'footnote-ref', label: 'note', number: 3 }]);
  });
  it('显形时露出源码，[^ 与 ] 变灰', () => {
    const r = fn('正文[^note]。', [4]);
    expect(r.widgets).toEqual([]);
    expect(r.gray).toEqual(['[^', ']']);
    expect(r.marks('cm-td-footnote-ref-src')).toEqual(['[^note]']);
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
    expect(structural(on.lines)).toEqual(['1:cm-td-h cm-td-h2']);
  });
  it('标题行只带字号类名，上下外边距交给前后的空行', () => {
    expect(structural(specs('# a\n\n## b\n\nc\n\n### d', [99]).lines)).toEqual(['1:cm-td-h cm-td-h1', '3:cm-td-h cm-td-h2', '7:cm-td-h cm-td-h3']);
  });
  it('列表符号、任务框始终换成 widget（连同前面的缩进），与光标无关', () => {
    const d = '- 一\n  - 二\n1. 三\n- [x] 四';
    for (const p of [0, 3, d.length]) {
      expect(specs(d, [p]).widgets).toEqual(['bullet:- ', 'bullet:  - ', 'ordered:1. ', 'task:- [x] ']);
    }
    expect(specs(d, [0]).widgetSpecs).toEqual([
      { type: 'bullet', depth: 1, shift: 0 },
      { type: 'bullet', depth: 2, shift: 0 },
      { type: 'ordered', number: 1, shift: 0 },
      { type: 'task', checked: true, offset: 2, shift: 0 },
    ]);
  });
  it('有序列表按首项的数字顺序编号（与 <ol start> 相同），不看各项自己写的数字', () => {
    expect(specs('3. a\n3. b\n9) c', [99]).widgetSpecs.map(w => (w.type === 'ordered' ? w.number : -1))).toEqual([3, 4, 9]);
  });
  it('列表行：按容器层数施加缩进；已完成任务的整项（含子项）变淡', () => {
    const r = specs('- [x] 完成\n  - 子项\n- [ ] 未完成', [99]);
    expect(r.lines).toEqual(['1:cm-td-nest cm-td-task-done', '2:cm-td-nest cm-td-task-done', '3:cm-td-nest']);
    // 首行另有文档首块的上外边距（列表 .5em）
    expect(r.styles).toEqual(['1:--td-indent:1;padding-top:var(--td-m-p)', '2:--td-indent:2', '3:--td-indent:1']);
  });
  it('引用：> 始终隐藏（同一行的多层合成一段），按行施加竖线所在的层', () => {
    const r = specs('> 外\n> > 内', [0]);
    expect(r.hidden).toEqual(['> ', '> > ']);
    expect(r.lines).toEqual(['1:cm-td-nest cm-td-quote', '2:cm-td-nest cm-td-quote']);
    expect(r.styles[1]).toContain('--td-indent:2');
    expect(r.styles[1]).toContain('calc(0 * var(--td-indent-step) + var(--td-quote-bar-left)) 0,calc(1 * var(--td-indent-step) + var(--td-quote-bar-left)) 0');
  });
  it('引用里的列表与列表里的引用：前缀整段换成符号，竖线落在引用自己那一层', () => {
    const a = specs('> - 项', [99]);
    expect(a.widgets).toEqual(['bullet:> - ']);
    expect(a.styles[0]).toContain('calc(0 * var(--td-indent-step)');
    const b = specs('- > 引', [99]);
    expect(b.widgetSpecs).toEqual([{ type: 'bullet', depth: 1, shift: 1 }]);
    expect(b.styles[0]).toContain('calc(1 * var(--td-indent-step)');
  });
  it('列表项的续行隐藏到内容列；续行里的代码块只隐藏到内容列', () => {
    const r = specs('- 第一行\n  续行\n\n  ```\n    code\n  ```', [99]);
    expect(r.hidden).toEqual(['  ', '  ', '  ', '  ']);
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

describe('块间空行（段距）', () => {
  it('块之间的空行压成段距；代码块、公式块里的空行不算', () => {
    const r = specs('a\n\nb\n\n```\n\n```\n\n$$\n\n$$', [0]);
    expect(r.lines.filter(l => l.endsWith(GAP_LINE))).toEqual([`2:${GAP_LINE}`, `4:${GAP_LINE}`, `8:${GAP_LINE}`]);
  });
  it('引用里只有 > 的行、松散列表项之间的空行也是段距；只有符号的空列表项不是', () => {
    const r = specs('> a\n>\n> b\n\n- x\n\n- y\n- ', [0]);
    expect(r.lines.filter(l => l.includes(GAP_LINE)).map(l => l.split(':')[0])).toEqual(['2', '4', '6']);
  });
  it('空行高度按前后两块在旧编辑器里的外边距：可折叠的取大者，代码块（inline-flex）的相加；段落之间用默认值', () => {
    const gaps = (d: string) => specs(d, [d.length]).styles.filter(s => s.includes('--td-gap-h'));
    expect(gaps('a\n\nb')).toEqual([]);
    expect(gaps('# a\n\n## b')).toEqual(['2:--td-gap-h:var(--td-m-h)']);
    expect(gaps('a\n\n# b')).toEqual(['2:--td-gap-h:max(var(--td-m-p), var(--td-m-h))']);
    expect(gaps('a\n\n| x |\n| - |\n\nb')).toEqual(['2:--td-gap-h:max(var(--td-m-p), var(--td-m-fig))', '5:--td-gap-h:max(var(--td-m-fig), var(--td-m-p))']);
    expect(gaps('```\nx\n```\n\n```\ny\n```')).toEqual(['4:--td-gap-h:calc(var(--td-m-code) + var(--td-m-code))']);
    expect(gaps('a\n\n```\nx\n```')).toEqual(['2:--td-gap-h:calc(var(--td-m-p) + var(--td-m-code))']);
    // 列表项之间的空行落在列表内部：段落间距
    expect(gaps('- a\n\n- b')).toEqual([]);
  });
  it('连续多个空行：按 Muya 生成的空段落数平分总高度（n 个换行 → ⌊n/2⌋ − 1 个空段落）', () => {
    const gaps = (d: string) => specs(d, [d.length]).styles.filter(s => s.includes('--td-gap-h'));
    // 两个空行：没有空段落，两行平分一个段距
    expect(gaps('a\n\n\nb')).toEqual(['2:--td-gap-h:calc((var(--td-m-p)) / 2)', '3:--td-gap-h:calc((var(--td-m-p)) / 2)']);
    // 三个空行：一个空段落（一行正文高，上下各一个段距）
    const three = gaps('a\n\n\n\nb');
    expect(three).toHaveLength(3);
    expect(three[0]).toBe('2:--td-gap-h:calc((calc(var(--td-m-p) + 1 * var(--td-lh-px) + 0 * var(--td-m-p) + var(--td-m-p))) / 3)');
  });
  it('没有空行的相邻块：间距加成文字行的内边距（优先加在前一块末行）', () => {
    const pads = (d: string) => specs(d, [d.length]).styles.filter(s => s.includes('padding'));
    // 首行同时带文档首块的上外边距
    expect(pads('# a\nb')).toEqual(['1:padding-top:var(--td-m-h);padding-bottom:max(var(--td-m-h), var(--td-m-p))']);
    expect(pads('x\n\na\n```\nc\n```')).toContain('3:padding-bottom:calc(var(--td-m-p) + var(--td-m-code))');
    // 前一块是整块 widget（代码块）时加在后一块首行
    expect(pads('x\n\n```\nc\n```\nb')).toContain('6:padding-top:calc(var(--td-m-code) + var(--td-m-p))');
  });
});
