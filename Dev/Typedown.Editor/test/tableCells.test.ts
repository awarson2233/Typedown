import { describe, expect, it } from 'vitest';
import { cellEdit, escapeCell, minimalChange, parseTable, splitRow, tableRows, unescapeCell } from '../src/editor/commands/tableCells';
import { iterateTopBlocks } from '../src/editor/syntax';
import { fullTree, stateOf } from './helpers';
import { gfmExamples, label } from './spec';

const cells = (line: string) => splitRow(line).map(c => line.slice(c.from, c.to));

describe('splitRow', () => {
  it('两侧竖线可有可无，内容去掉两侧空白', () => {
    expect(cells('| a | b |')).toEqual(['a', 'b']);
    expect(cells('a | b')).toEqual(['a', 'b']);
    expect(cells('|a|b')).toEqual(['a', 'b']);
    expect(cells('  | a  |   b |  ')).toEqual(['a', 'b']);
  });
  it('转义的竖线不拆分', () => {
    expect(cells('| a \\| b | c |')).toEqual(['a \\| b', 'c']);
    expect(cells('| `x \\| y` | z |')).toEqual(['`x \\| y`', 'z']);
  });
  it('空单元格是插入点：在左竖线后第一个空格之后', () => {
    const line = '| a |   | c |';
    const r = splitRow(line);
    expect(r[1].from).toBe(r[1].to);
    expect(line.slice(0, r[1].from)).toBe('| a | ');
    expect(splitRow('|a||c|')[1]).toEqual({ from: 3, to: 3 });
  });
});

describe('parseTable', () => {
  const src = '| 左 | 中 | 右 |\n|:--|:-:|--:|\n| 1 | 2 | 3 |\n| x |';
  it('区间是文档偏移，对齐来自分隔行，表体行可以比表头短', () => {
    const m = parseTable(src, 100);
    expect(m.cols).toBe(3);
    expect(m.aligns).toEqual(['left', 'center', 'right']);
    expect(src.slice(m.body[0].cells[2].from - 100, m.body[0].cells[2].to - 100)).toBe('3');
    expect(m.body[1].cells).toHaveLength(1);
  });
});

describe('单元格编辑只改该单元格的源码区间', () => {
  const src = '|  姓名  | 城市|\n|---|---:|\n| 张三   |北京 |\n|李四|  上海   |';
  const apply = (s: string, c: { from: number; to: number; insert: string } | null) => (c ? s.slice(0, c.from) + c.insert + s.slice(c.to) : s);
  const edit = (row: number, col: number, text: string) => {
    const m = parseTable(src, 0);
    return apply(src, cellEdit((f, t) => src.slice(f, t), m, row, col, text));
  };

  it('其他单元格与对齐空格字节不变', () => {
    const out = edit(1, 0, '张三丰');
    expect(out).toBe('|  姓名  | 城市|\n|---|---:|\n| 张三丰   |北京 |\n|李四|  上海   |');
    const before = parseTable(src, 0), after = parseTable(out, 0);
    tableRows(before).forEach((row, r) => row.cells.forEach((c, k) => {
      if (r === 1 && k === 0) return;
      const a = tableRows(after)[r].cells[k];
      expect(out.slice(a.from, a.to)).toBe(src.slice(c.from, c.to));
    }));
  });
  it('最小替换：只替换新旧文本不同的中段', () => {
    expect(minimalChange('张三', '张三丰', 10)).toEqual({ from: 12, to: 12, insert: '丰' });
    expect(minimalChange('abc', 'abc', 0)).toBeNull();
    expect(minimalChange('abXc', 'abc', 0)).toEqual({ from: 2, to: 3, insert: '' });
  });
  it('竖线转义为 \\|，换行变空格，已转义的保持', () => {
    expect(escapeCell('a|b')).toBe('a\\|b');
    expect(escapeCell('a\\|b')).toBe('a\\|b');
    expect(escapeCell('a\nb')).toBe('a b');
    expect(unescapeCell('a\\|b')).toBe('a|b');
    expect(edit(2, 1, '上|海')).toBe('|  姓名  | 城市|\n|---|---:|\n| 张三   |北京 |\n|李四|  上\\|海   |');
  });
  it('清空单元格只删内容，两侧空白保留', () => {
    expect(edit(1, 1, '')).toBe('|  姓名  | 城市|\n|---|---:|\n| 张三   | |\n|李四|  上海   |');
  });
  it('缺的单元格在行尾补齐', () => {
    const s = '| a | b | c |\n|---|---|---|\n| 1 |';
    const m = parseTable(s, 0);
    expect(apply(s, cellEdit((f, t) => s.slice(f, t), m, 1, 2, 'z'))).toBe('| a | b | c |\n|---|---|---|\n| 1 |  | z |');
  });
});

describe('GFM 规范里的每张表：逐格追加字符，只有该格变化', () => {
  const tables = gfmExamples.filter(e => e.extension === 'table');
  it(`覆盖 ${tables.length} 个表格用例`, () => {
    expect(tables.length).toBeGreaterThan(5);
    const bad: string[] = [];
    for (const e of tables) {
      const state = stateOf(e.markdown);
      iterateTopBlocks(fullTree(state), 0, state.doc.length, n => {
        if (n.name !== 'Table') return;
        const from = state.doc.lineAt(n.from).from, to = state.doc.lineAt(n.to).to;
        const src = state.sliceDoc(from, to);
        const model = parseTable(src, from);
        tableRows(model).forEach((row, r) => row.cells.forEach((cell, c) => {
          const change = cellEdit((f, t) => state.sliceDoc(f, t), model, r, c, unescapeCell(state.sliceDoc(cell.from, cell.to)) + 'Z');
          const doc = state.doc.toString();
          const got = change ? doc.slice(0, change.from) + change.insert + doc.slice(change.to) : doc;
          if (got !== doc.slice(0, cell.to) + 'Z' + doc.slice(cell.to)) bad.push(`${label(e)} r${r}c${c}`);
        }));
      });
    }
    expect(bad).toEqual([]);
  });
});
