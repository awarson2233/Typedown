// @vitest-environment jsdom
import { beforeAll, describe, expect, it } from 'vitest';
import { EditorSelection, EditorState } from '@codemirror/state';
import { EditorView } from '@codemirror/view';
import { Language, LanguageSupport, ensureSyntaxTree } from '@codemirror/language';
import { commonmarkLanguage } from '@codemirror/lang-markdown';
import type { MarkdownConfig, MarkdownParser } from '@lezer/markdown';
import { createEditor, typoraLayer, type TypedownEditor } from '../src/editor/createEditor';
import { markdownExtensions } from '../src/editor/syntax';
import { footnoteNumberSource } from '../src/editor/decorations/inlinePlugin';
import { normalizeFootnoteLabel } from '../src/editor/syntax/footnote';
import { linkAt, bareUrlHref } from '../src/editor/decorations/links';
import { fullTree, stateOf } from './helpers';

/**
 * 行内显形在真实视图里的行为（jsdom：没有布局，但装饰、widget 的 toDOM 与事件处理都真实运行）：
 * 任务框点击、行内 HTML 的消毒、列表符号的 DOM、组字时块间空行的处理。
 */

beforeAll(() => {
  const rect = () => ({ left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0, toJSON() {} }) as DOMRect;
  const rects = () => Object.assign([], { item: () => null }) as unknown as DOMRectList;
  Range.prototype.getBoundingClientRect = rect;
  Range.prototype.getClientRects = rects;
  Element.prototype.getClientRects = rects;
});

function mount(doc: string, cursor = doc.length): TypedownEditor {
  const parent = document.createElement('div');
  document.body.appendChild(parent);
  const ed = createEditor({ doc, parent });
  ensureSyntaxTree(ed.view.state, doc.length, 1e9);
  ed.view.dispatch({ selection: EditorSelection.cursor(cursor) });
  return ed;
}

describe('任务框', () => {
  it('点击只翻转 [ ] 与 [x] 中间的那个字符，嵌在引用里也一样', () => {
    const ed = mount('> - [ ] 任务\n- [X] 已完成');
    const boxes = () => Array.from(ed.view.contentDOM.querySelectorAll<HTMLInputElement>('input.cm-td-task'));
    expect(boxes().map(b => b.checked)).toEqual([false, true]);
    boxes()[0].dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
    expect(ed.getText()).toBe('> - [x] 任务\n- [X] 已完成');
    boxes()[1].dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
    expect(ed.getText()).toBe('> - [x] 任务\n- [ ] 已完成');
    expect(boxes().map(b => b.checked)).toEqual([true, false]);
    ed.view.destroy();
  });
});

describe('列表符号', () => {
  it('无序列表按层次取 disc / circle / square，有序列表以 <ol start> 给出序号', () => {
    const ed = mount('- a\n  - b\n    - c\n\n7. x\n8. y');
    const lists = Array.from(ed.view.contentDOM.querySelectorAll<HTMLElement>('.cm-td-marker-list'));
    expect(lists.map(l => (l.tagName === 'OL' ? `ol:${(l as HTMLOListElement).start}` : `ul:${l.style.listStyleType}`)))
      .toEqual(['ul:disc', 'ul:circle', 'ul:square', 'ol:7', 'ol:8']);
    // 前缀整段被 widget 替换，行内看不到 `-` 与缩进
    expect(ed.view.contentDOM.querySelector('.cm-line')!.textContent).toBe('a');
    ed.view.destroy();
  });
});

describe('行内 HTML', () => {
  it('内容包进真实元素；属性经 DOMPurify 消毒，事件处理器与 id 被去掉', () => {
    const ed = mount('a<u>下</u><span style="color:red" onclick="alert(1)" id="x">红</span><a href="javascript:alert(1)">链</a>', 0);
    const u = ed.view.contentDOM.querySelector('u.cm-td-html-inline');
    expect(u?.textContent).toBe('下');
    const span = ed.view.contentDOM.querySelector('span.cm-td-html-inline') as HTMLElement;
    expect(span.textContent).toBe('红');
    expect(span.style.color).toBe('red');
    expect(span.hasAttribute('onclick')).toBe(false);
    expect(span.hasAttribute('id')).toBe(false);
    const a = ed.view.contentDOM.querySelector('a.cm-td-html-inline')!;
    expect(a.getAttribute('href')).toBeNull();
    // 不显形时标签源码不在 DOM 文本里
    expect(ed.view.contentDOM.textContent).toBe('a下红链');
    ed.view.destroy();
  });
});

describe('块间空行与组字', () => {
  it('空行带段距类名；组字在它上面开始时去掉该行的段距类名，行的其余类名保留', () => {
    const ed = mount('> a\n>\n> b', 0);
    const line2 = () => ed.view.contentDOM.querySelectorAll('.cm-line')[1] as HTMLElement;
    expect(line2().classList.contains('cm-td-gap')).toBe(true);
    expect(line2().classList.contains('cm-td-quote')).toBe(true);
    ed.view.dispatch({ changes: { from: 5, insert: ' 字' }, selection: { anchor: 7 }, userEvent: 'input.type.compose' });
    expect(line2().classList.contains('cm-td-gap')).toBe(false);
    expect(line2().classList.contains('cm-td-quote')).toBe(true);
    ed.view.destroy();
  });
});

describe('脚注编号', () => {
  // 脚注语法由块组件一侧提供；这里用只在测试里定义、产出同名节点的扩展，在真实视图里验证编号的注入与标签规范化
  const TestFootnote: MarkdownConfig = {
    defineNodes: ['FootnoteReference', 'FootnoteReferenceMark', 'FootnoteLabel'],
    parseInline: [{
      name: 'FootnoteReference',
      before: 'Link',
      parse(cx, next, pos) {
        if (next !== 91 || cx.char(pos + 1) !== 94) return -1;
        const m = /^\[\^([^\]]+)\]/.exec(cx.slice(pos, cx.end));
        if (!m) return -1;
        const end = pos + m[0].length;
        return cx.addElement(cx.elt('FootnoteReference', pos, end, [
          cx.elt('FootnoteReferenceMark', pos, pos + 2), cx.elt('FootnoteLabel', pos + 2, end - 1), cx.elt('FootnoteReferenceMark', end - 1, end),
        ]));
      },
    }],
  };
  const lang = new LanguageSupport(new Language(commonmarkLanguage.data, (commonmarkLanguage.parser as MarkdownParser).configure([...markdownExtensions, TestFootnote]), [], 'markdown'));

  it('规范化规则：大小写折叠、连续空白合并、去掉首尾空白', () => {
    expect(normalizeFootnoteLabel('  Foo \t Bar ')).toBe('foo bar');
    expect(normalizeFootnoteLabel('ẞ')).toBe(normalizeFootnoteLabel('ss'));
  });
  it('按规范化后的标签查编号；查不到时显示标签原文', () => {
    const doc = '正文[^Note  A]与[^other]。';
    const numbers = new Map([['note a', 2]]);
    const parent = document.createElement('div');
    document.body.appendChild(parent);
    const view = new EditorView({
      parent,
      state: EditorState.create({ doc, selection: { anchor: 0 }, extensions: [lang, typoraLayer, footnoteNumberSource.of(() => numbers)] }),
    });
    ensureSyntaxTree(view.state, doc.length, 1e9);
    view.dispatch({});
    const sups = Array.from(view.contentDOM.querySelectorAll('sup.cm-td-footnote-ref')).map(s => s.textContent);
    expect(sups).toEqual(['2', 'other']);
    view.destroy();
  });
});

describe('链接定位', () => {
  it('行内链接、尖括号自动链接、裸链接给出可打开的地址；引用式链接没有地址', () => {
    const at = (doc: string, pos: number) => { const s = stateOf(doc); return linkAt(fullTree(s), s.doc, pos, 1); };
    expect(at('看[文字](https://e.com)吧', 2)).toMatchObject({ from: 1, to: 20, textEnd: 4, url: 'https://e.com' });
    expect(at('看[文字](https://e.com)吧', 8)?.url).toBe('https://e.com');
    expect(at('<https://e.com>', 3)).toMatchObject({ textEnd: null, url: 'https://e.com' });
    expect(at('<a@b.com>', 3)?.url).toBe('mailto:a@b.com');
    expect(at('见 www.e.com 吧', 4)?.url).toBe('http://www.e.com');
    expect(at('[a][r]\n\n[r]: /x', 1)).toMatchObject({ url: null, textEnd: 2 });
    expect(at('纯文本', 1)).toBeNull();
    expect(bareUrlHref('a@b.com')).toBe('mailto:a@b.com');
  });
});
