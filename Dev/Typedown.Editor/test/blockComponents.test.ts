// @vitest-environment jsdom
import { beforeAll, describe, expect, it } from 'vitest';
import { EditorState, type Extension } from '@codemirror/state';
import { Decoration, EditorView } from '@codemirror/view';
import { ensureSyntaxTree } from '@codemirror/language';
import { CompletionContext } from '@codemirror/autocomplete';
import { markdownSupport } from '../src/editor/syntax';
import { normalizeFootnoteLabel } from '../src/editor/syntax/footnote';
import { revealFrozen } from '../src/editor/decorations/revealState';
import { outlineField } from '../src/editor/state/outline';
import { footnoteField, footnoteNumber, footnoteNumbers, firstReference, scanFootnotes, updateFootnotes } from '../src/editor/state/footnotes';
import { documentLocation } from '../src/editor/state/documentLocation';
import { blockField } from '../src/editor/widgets/blockField';
import { DiagramWidget, TocWidget } from '../src/editor/widgets/blockWidgets';
import { ImageWidget, forgetImageStates, imageAltEnd } from '../src/editor/widgets/imageWidget';
import { fenceGuards, GuardKind, hiddenRangesAround, normalizeCursor, protectedChar } from '../src/editor/blocks/fenceGuards';
import { languageCompletionSource } from '../src/editor/blocks/languageCompletion';
import { highlightCode } from '../src/editor/blocks/codeHighlight';
import { codeLanguageId, findCodeLanguage } from '../src/editor/blocks/codeLanguage';
import { ImageSourceKind, resolveImageSource } from '../src/renderers/imageUrl';
import { isInvisibleHtml, sanitizeHtml } from '../src/renderers/html';
import { loadKatex } from '../src/renderers/katex';
import { rng } from './helpers';

beforeAll(() => {
  const rects = () => Object.assign([], { item: () => null }) as unknown as DOMRectList;
  Range.prototype.getClientRects = rects;
  Range.prototype.getBoundingClientRect = () => ({ left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0, toJSON() {} }) as DOMRect;
  (globalThis as { ResizeObserver?: unknown }).ResizeObserver ??= class { observe() {} unobserve() {} disconnect() {} };
});

const BASE = 'C:\\docs\\notes';
const base: Extension[] = [markdownSupport(), revealFrozen, outlineField, footnoteField, blockField, documentLocation.of({ basePath: BASE })];

function create(doc: string, cursor = 0, extra: Extension[] = []) {
  let state = EditorState.create({ doc, extensions: [base, extra], selection: { anchor: Math.min(cursor, doc.length) } });
  ensureSyntaxTree(state, doc.length, 1e9);
  state = state.update({}).state;
  return state;
}

function view(doc: string, cursor = 0, extra: Extension[] = []) {
  const parent = document.createElement('div');
  document.body.appendChild(parent);
  return new EditorView({ state: create(doc, cursor, extra), parent });
}

const mouse = (el: Element) => el.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, cancelable: true, button: 0 }));

describe('图片地址解析', () => {
  const local = (raw: string, basePath = BASE) => {
    const r = resolveImageSource(raw, basePath);
    return r.kind === ImageSourceKind.Local ? r.url : ImageSourceKind[r.kind];
  };
  it('相对路径按文档目录解析，. 与 .. 在页面内消解', () => {
    expect(local('images/a.png')).toBe('https://typedown.image/C/docs/notes/images/a.png');
    expect(local('./images/../b.png')).toBe('https://typedown.image/C/docs/notes/b.png');
    expect(local('../../x.png')).toBe('https://typedown.image/C/x.png');
    expect(local('../../../x.png')).toBe('Unsupported'); // 越过盘符根目录
    expect(local('/root.png')).toBe('https://typedown.image/C/root.png');
  });
  it('路径段逐段编码，百分号转义先解码', () => {
    expect(local('图 1.png')).toBe('https://typedown.image/C/docs/notes/%E5%9B%BE%201.png');
    expect(local('my%20image.png')).toBe('https://typedown.image/C/docs/notes/my%20image.png');
    expect(local('a%23b.png')).toBe('https://typedown.image/C/docs/notes/a%23b.png');
    expect(local('<a b.png>')).toBe('https://typedown.image/C/docs/notes/a%20b.png');
  });
  it('绝对路径、file://、长路径前缀', () => {
    expect(local('d:\\pics\\a.png')).toBe('https://typedown.image/D/pics/a.png');
    expect(local('D:/pics/a.png')).toBe('https://typedown.image/D/pics/a.png');
    expect(local('file:///D:/pics/a%20b.png')).toBe('https://typedown.image/D/pics/a%20b.png');
    expect(local('\\\\?\\D:\\pics\\a.png')).toBe('https://typedown.image/D/pics/a.png');
  });
  it('网络、空、UNC、其他协议', () => {
    expect(resolveImageSource('https://x.com/a.png', BASE)).toEqual({ kind: ImageSourceKind.Remote, url: 'https://x.com/a.png' });
    expect(resolveImageSource('data:image/png;base64,AA', BASE).kind).toBe(ImageSourceKind.Remote);
    expect(resolveImageSource('  ', BASE).kind).toBe(ImageSourceKind.Empty);
    expect(local('\\\\server\\share\\a.png')).toBe('Unsupported');
    expect(local('//server/share/a.png')).toBe('Unsupported');
    expect(local('file://server/share/a.png')).toBe('Unsupported');
    expect(local('javascript:alert(1)')).toBe('Unsupported');
    expect(local('a.png', '')).toBe('Unsupported'); // 没有文档目录
  });
});

describe('HTML 块', () => {
  it('只含不可见内容的块', () => {
    expect(isInvisibleHtml('<script>\nconsole.log(1)\n</script>')).toBe(true);
    expect(isInvisibleHtml('<!-- 注释 -->\n<style>p{}</style>')).toBe(true);
    expect(isInvisibleHtml('<meta charset="utf-8">')).toBe(true);
    expect(isInvisibleHtml('<div>x</div>')).toBe(false);
    expect(isInvisibleHtml('<script>1</script>\n<b>x</b>')).toBe(false);
  });
  it('消毒去掉脚本、事件、style，图片地址改写', async () => {
    const html = await sanitizeHtml('<div onclick="x()" style="color:red" contenteditable data-x="1"><img src="a.png" onerror="y()"><script>1</script><style>p{}</style><img src="\\\\srv\\a.png"></div>', BASE);
    expect(html).not.toMatch(/onclick|onerror|style|script|contenteditable|data-x/);
    expect(html).toContain('<img src="https://typedown.image/C/docs/notes/a.png">');
    expect(html).toContain('<img>'); // UNC 的 src 被去掉
  });
});

describe('脚注编号', () => {
  it('label 规范化：大小写折叠、空白合并', () => {
    expect(normalizeFootnoteLabel(' Note ')).toBe(normalizeFootnoteLabel('note'));
    expect(normalizeFootnoteLabel('a  b\tc')).toBe(normalizeFootnoteLabel('A B C'));
    expect(normalizeFootnoteLabel('ẞ')).toBe(normalizeFootnoteLabel('ß'));
  });
  it('按引用首次出现编号，未引用的定义排后，大小写不同的是同一个脚注', () => {
    const s = create('正文[^b]与[^A]，再[^B]。\n\n[^a]: 甲\n\n[^b]: 乙\n\n[^c]: 丙\n');
    expect([...footnoteNumbers(s)]).toEqual([[normalizeFootnoteLabel('b'), 1], [normalizeFootnoteLabel('a'), 2], [normalizeFootnoteLabel('c'), 3]]);
    expect(footnoteNumber(s, 'a')).toBe(2);
    expect(footnoteNumber(s, 'B')).toBe(1);
    expect(footnoteNumber(s, 'zz')).toBe(0);
    expect(firstReference(s, 'a')).toBe(s.doc.toString().indexOf('[^A]'));
  });
  it('增量维护 ≡ 全量重扫（随机编辑）', () => {
    const r = rng(7);
    const pieces = ['[^a]', '[^B]', '[^b]:', '\n', '\n\n', 'x', ' ', ':', '[', '^', ']', '> [^c]: y'];
    let state = create('开头[^a]\n\n[^a]: 甲\n');
    let idx = state.field(footnoteField);
    for (let i = 0; i < 400; i++) {
      const len = state.doc.length;
      const from = Math.floor(r() * (len + 1));
      const to = r() < 0.3 ? Math.min(len, from + Math.floor(r() * 6)) : from;
      const insert = r() < 0.8 ? pieces[Math.floor(r() * pieces.length)] : '';
      const tr = state.update({ changes: { from, to, insert } });
      idx = updateFootnotes(idx, tr.state.doc, tr.changes);
      state = tr.state;
      const full = scanFootnotes(state.doc);
      expect(idx.mentions, `第 ${i} 次`).toEqual(full.mentions);
      expect([...idx.numbers]).toEqual([...full.numbers]);
      expect(state.field(footnoteField).mentions).toEqual(full.mentions);
    }
  });
});

describe('围栏保护', () => {
  const doc = '段落\n\n```js\ncode\n```\n\n$$\nx\n$$\n\n尾';
  const at = (s: string) => doc.indexOf(s);
  it('隐藏范围：代码块的 ``` 前缀与闭围栏、公式块两条围栏', () => {
    const s = create(doc, doc.length);
    const kinds = hiddenRangesAround(s, 0, doc.length).map(r => `${GuardKind[r.kind]}@${r.from}-${r.to}`);
    expect(kinds).toContain(`Prefix@${at('```js')}-${at('js')}`);
    expect(kinds).toContain(`CloseLine@${at('```\n\n$$')}-${at('```\n\n$$') + 3}`);
  });
  it('光标规范化：按方向挪出隐藏范围，单击闭围栏回到块尾', () => {
    const s = create(doc, at('code'));
    const ranges = hiddenRangesAround(s, 0, doc.length);
    const open = at('```js');
    expect(normalizeCursor(s.doc, ranges, open - 1, open, false)).toBe(at('js'));
    expect(normalizeCursor(s.doc, ranges, at('js'), open + 1, false)).toBe(open - 1);
    const close = at('```\n\n$$');
    expect(normalizeCursor(s.doc, ranges, 0, close + 1, true)).toBe(close - 1);
    expect(normalizeCursor(s.doc, ranges, 0, at('code'), false)).toBe(at('code'));
  });
  it('事务过滤器：空光标落进隐藏范围时挪出，选区不动', () => {
    let s = create(doc, at('code'), [fenceGuards]);
    const close = at('```\n\n$$');
    s = s.update({ selection: { anchor: close + 2 }, userEvent: 'select' }).state;
    expect(s.selection.main.head).toBe(close + 4); // 从前面移入 → 闭围栏行之后
    s = s.update({ selection: { anchor: 0, head: doc.length } }).state;
    expect([s.selection.main.from, s.selection.main.to]).toEqual([0, doc.length]);
  });
  it('删除保护：隐藏字符与围栏行两端的换行', () => {
    const s = create(doc, at('code'));
    const close = at('```\n\n$$');
    expect(protectedChar(s, at('```js') + 1).blocked).toBe(true);
    expect(protectedChar(s, close - 1)).toEqual({ blocked: true, moveTo: -1 }); // 闭围栏行前的换行
    expect(protectedChar(s, close + 3)).toEqual({ blocked: true, moveTo: close - 1 }); // 闭围栏之后的换行：光标移进块尾
    expect(protectedChar(s, at('code') + 1).blocked).toBe(false);
  });
});

describe('代码块语言', () => {
  it('补全只在开围栏行的信息串上出现', () => {
    const doc = '```py\ncode\n```\n\n段落 ```x';
    const s = create(doc);
    const r = languageCompletionSource(new CompletionContext(s, 5, false));
    expect(r?.from).toBe(3);
    expect(r?.to).toBe(5);
    expect(r?.options.some(o => o.label === 'python')).toBe(true);
    expect(r?.options.some(o => o.label === 'mermaid')).toBe(true);
    expect(languageCompletionSource(new CompletionContext(s, 13, true))).toBeNull(); // 闭围栏
    expect(languageCompletionSource(new CompletionContext(s, doc.length, true))).toBeNull(); // 段落里的反引号
  });
  it('语言 id 与 prism 类名', async () => {
    expect(codeLanguageId('js')).toBe('javascript');
    expect(codeLanguageId('C#')).toBe('csharp');
    expect(codeLanguageId('无此语言')).toBeNull();
    await findCodeLanguage('javascript')!.load();
    const tokens = highlightCode('javascript', 'const a = "s"; // c')!;
    const text = 'const a = "s"; // c';
    const cls = (w: string) => tokens.find(([f, t]) => text.slice(f, t) === w)?.[2];
    expect(cls('const')).toBe('token keyword');
    expect(cls('"s"')).toBe('token string');
    expect(cls('// c')).toBe('token comment');
    expect(highlightCode('没有的语言', 'x')).toEqual([]);
  });
});

describe('图片 widget', () => {
  it('空、不支持、本地三种形态', () => {
    forgetImageStates();
    const v = view('![a](x.png)');
    const dom = (src: string) => new ImageWidget({ src, alt: 'a', title: null }).toDOM(v);
    expect(dom('').className).toBe('cm-td-image cm-td-image-empty');
    expect(dom('\\\\srv\\a.png').className).toBe('cm-td-image cm-td-image-fail');
    const ok = dom('images/a.png');
    expect(ok.className).toBe('cm-td-image cm-td-image-loading');
    expect(ok.querySelector('img')!.getAttribute('src')).toBe('https://typedown.image/C/docs/notes/images/a.png');
    ok.querySelector('img')!.dispatchEvent(new Event('error'));
    expect(ok.className).toBe('cm-td-image cm-td-image-fail');
    v.destroy();
  });
  it('alt 末尾：widget 在图片起点或终点都能找到', () => {
    const doc = '前 ![替代 *字*](a.png "t") 后';
    const s = create(doc);
    const from = doc.indexOf('!'), to = doc.indexOf(')') + 1;
    expect(imageAltEnd(s, from)).toBe(doc.indexOf(']'));
    expect(imageAltEnd(s, to)).toBe(doc.indexOf(']'));
    expect(imageAltEnd(s, 0)).toBeNull();
  });
  it('单击图片把光标放到 alt 末尾', () => {
    const doc = '前 ![logo](a.png) 后';
    const from = doc.indexOf('!'), to = doc.indexOf(')') + 1;
    const widget = new ImageWidget({ src: 'a.png', alt: 'logo', title: null });
    const v = view(doc, 0, [EditorView.decorations.of(Decoration.set([Decoration.replace({ widget }).range(from, to)]))]);
    mouse(v.dom.querySelector('.cm-td-image')!);
    expect(v.state.selection.main.head).toBe(doc.indexOf(']'));
    v.destroy();
  });
});

describe('块 widget', () => {
  it('[TOC] 单击条目把光标放到标题行尾', () => {
    const doc = '[TOC]\n\n# 一\n\n## 二\n\n正文';
    const v = view(doc, doc.length);
    const toc = v.dom.querySelector('.cm-td-toc')!;
    expect([...toc.querySelectorAll('.cm-td-toc-link')].map(a => a.textContent)).toEqual(['一', '二']);
    mouse(toc.querySelectorAll('.cm-td-toc-link')[1]);
    expect(v.state.selection.main.head).toBe(doc.indexOf('## 二') + 4);
    v.destroy();
  });
  it('[TOC] 没有标题时显示占位', () => {
    const v = view('[TOC]\n\n正文', 10);
    const toc = new TocWidget([]).toDOM(v);
    expect(toc.querySelector('.cm-td-toc-empty')).not.toBeNull();
    v.destroy();
  });
  it('公式与 mermaid 的空块、出错占位', async () => {
    await loadKatex();
    const v = view('x');
    expect(new DiagramWidget('math', '', false).toDOM(v).querySelector('.cm-td-render-empty')).not.toBeNull();
    expect(new DiagramWidget('math', '\\frac{', false).toDOM(v).querySelector('.cm-td-render-error')).not.toBeNull();
    expect(new DiagramWidget('mermaid', '  ', false).toDOM(v).querySelector('.cm-td-render-empty')).not.toBeNull();
    v.destroy();
  });
});
