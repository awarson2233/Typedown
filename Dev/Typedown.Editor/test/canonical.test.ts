import { describe, expect, it } from 'vitest';
import { canonicalHead, headingTextStart, hiddenPrefixEnd, whitespaceInsertPos } from '../src/editor/decorations/canonical';
import { fullTree, stateOf } from './helpers';

const prefix = (doc: string, lineNo: number) => {
  const s = stateOf(doc);
  const line = s.doc.line(lineNo);
  return hiddenPrefixEnd(fullTree(s), line) - line.from;
};

describe('行首隐藏前缀', () => {
  it('列表、任务、引用、嵌套', () => {
    expect(prefix('- 项', 1)).toBe(2);
    expect(prefix('- [ ] 任务', 1)).toBe(6);
    expect(prefix('> 引用', 1)).toBe(2);
    expect(prefix('- a\n  - 嵌套', 2)).toBe(4);
    expect(prefix('> - [x] 引用里的任务', 1)).toBe(8);
    expect(prefix('1. 有序', 1)).toBe(3);
  });
  it('普通段落与标题没有常隐藏前缀', () => {
    expect(prefix('正文', 1)).toBe(0);
    expect(prefix('# 标题', 1)).toBe(0);
  });
  it('标题文字起点', () => {
    const s = stateOf('###  标题\n正文');
    expect(headingTextStart(fullTree(s), s.doc.line(1))).toBe(5);
    expect(headingTextStart(fullTree(s), s.doc.line(2))).toBe(-1);
  });
});

describe('选区规范化 canonicalHead', () => {
  const doc = '段落\n- 列表项\n## 标题\n末行';
  // 行首偏移：段落 0，列表 3（内容起点 5），标题 9（文字起点 12），末行 15
  const s = stateOf(doc);
  const t = fullTree(s);
  it('落在列表前缀内 → 前缀末尾（单击行首、Home、上下移动）', () => {
    expect(canonicalHead(s.doc, t, 0, 3)).toBe(5);
    expect(canonicalHead(s.doc, t, 12, 4)).toBe(5);
  });
  it('从内容起点向左 → 越过前缀到上一行行尾', () => {
    expect(canonicalHead(s.doc, t, 5, 3)).toBe(2);
  });
  it('从别的行移入标题行首 → 标题文字前；同一行内移动不改', () => {
    expect(canonicalHead(s.doc, t, 8, 9)).toBe(12);
    expect(canonicalHead(s.doc, t, 12, 9)).toBe(9);
  });
  it('普通位置不变', () => {
    expect(canonicalHead(s.doc, t, 0, 1)).toBe(1);
    expect(canonicalHead(s.doc, t, 16, 15)).toBe(15);
  });
});

describe('强调边缘的空白落到标记外', () => {
  const at = (doc: string, pos: number) => whitespaceInsertPos(fullTree(stateOf(doc)), pos);
  it('闭合标记内侧 → 标记外', () => {
    expect(at('**粗体**后', 4)).toBe(6);
    expect(at('*斜*后', 2)).toBe(3);
    expect(at('~~删~~', 3)).toBe(5);
  });
  it('开标记内侧 → 标记外', () => {
    expect(at('前**粗体**', 3)).toBe(1);
  });
  it('嵌套时逐层外移', () => {
    expect(at('***a***', 4)).toBe(7);
  });
  it('内容中间与标记外侧不动', () => {
    expect(at('**粗体**', 3)).toBe(3);
    expect(at('**粗体**', 6)).toBe(6);
    expect(at('a `b` c', 4)).toBe(4);
  });
});
