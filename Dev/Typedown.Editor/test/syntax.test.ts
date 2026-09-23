import { describe, expect, it } from 'vitest';
import { nodes, stateOf } from './helpers';
import { cjkFlanking, flanking } from '../src/editor/syntax/cjkEmphasis';

describe('行内公式', () => {
  it('识别 $…$ 与 $$…$$', () => {
    expect(nodes(stateOf('质能 $E=mc^2$ 方程与 $$\\sum x$$ 行内'), 'InlineMath')).toEqual(['$E=mc^2$', '$$\\sum x$$']);
  });
  it('金额写法不算公式', () => {
    expect(nodes(stateOf('价格 $5 与 $6 之间'), 'InlineMath')).toEqual([]);
    expect(nodes(stateOf('a $x $ b'), 'InlineMath')).toEqual([]);
    expect(nodes(stateOf('a $x$5 b'), 'InlineMath')).toEqual([]);
  });
  it('转义的 $ 不开公式', () => {
    expect(nodes(stateOf('a \\$x$ b'), 'InlineMath')).toEqual([]);
  });
  it('行内代码里的 $ 不是公式', () => {
    expect(nodes(stateOf('`$x$` 外面'), 'InlineMath')).toEqual([]);
  });
});

describe('块级公式', () => {
  it('独占一行的 $$ 围起来', () => {
    const s = stateOf('前文\n\n$$\n\\int_0^1 x\\,dx\n$$\n\n后文');
    expect(nodes(s, 'BlockMath')).toEqual(['$$\n\\int_0^1 x\\,dx\n$$']);
    expect(nodes(s, 'BlockMathMark')).toEqual(['$$', '$$']);
    expect(nodes(s, 'BlockMathContent')).toEqual(['\\int_0^1 x\\,dx']);
  });
  it('未闭合时延续到文末', () => {
    expect(nodes(stateOf('$$\na\nb'), 'BlockMath')).toEqual(['$$\na\nb']);
  });
  it('引用里的公式块', () => {
    const s = stateOf('> $$\n> x\n> $$\n');
    expect(nodes(s, 'BlockMath')).toEqual(['$$\n> x\n> $$']);
    expect(nodes(s, 'BlockMathContent')).toEqual(['x']);
  });
  it('多行内容逐行成节点', () => {
    expect(nodes(stateOf('$$\na\nb\n$$'), 'BlockMathContent')).toEqual(['a', 'b']);
  });
});

describe('==高亮==', () => {
  it('识别高亮', () => {
    expect(nodes(stateOf('这是 ==重点== 文字'), 'Highlight')).toEqual(['==重点==']);
    expect(nodes(stateOf('这是==重点==文字'), 'Highlight')).toEqual(['==重点==']);
  });
  it('三个等号或空白边界不算', () => {
    expect(nodes(stateOf('a === b'), 'Highlight')).toEqual([]);
    expect(nodes(stateOf('a == b == c'), 'Highlight')).toEqual([]);
  });
});

describe.each(['native', 'yaml'] as const)('front matter（%s）', frontmatter => {
  it('文首 --- 块解析为 Frontmatter，正文照常解析', () => {
    const s = stateOf('---\ntitle: 测试\ntags: [a, b]\n---\n\n# 标题\n', [], { frontmatter });
    // lang-yaml 的节点带上闭合行后的换行
    expect(nodes(s, 'Frontmatter').map(t => t.trimEnd())).toEqual(['---\ntitle: 测试\ntags: [a, b]\n---']);
    expect(nodes(s, 'ATXHeading1')).toEqual(['# 标题']);
  });
  it('不在文首的 --- 是分隔线', () => {
    const s = stateOf('正文\n\n---\n\n后文', [], { frontmatter });
    expect(nodes(s, 'Frontmatter')).toHaveLength(0);
    expect(nodes(s, 'HorizontalRule')).toEqual(['---']);
  });
});

describe('front matter（native）', () => {
  it('文首 --- 没有闭合时不是 front matter（lang-yaml 的实现会把全文当成 front matter）', () => {
    const s = stateOf('---\n\n正文', []);
    expect(nodes(s, 'Frontmatter')).toHaveLength(0);
    expect(nodes(s, 'HorizontalRule')).toEqual(['---']);
  });
  it('front matter 里的 # 与 --- 不当作 markdown', () => {
    const s = stateOf('---\n# 注释\na: 1\n---\n正文');
    expect(nodes(s, 'ATXHeading1')).toEqual([]);
    expect(nodes(s, 'FrontmatterContent')).toEqual(['# 注释', 'a: 1']);
  });
});

describe('CJK 强调边界', () => {
  const strong = (doc: string) => nodes(stateOf(doc), 'StrongEmphasis');
  it('标准规则下已经成立的写法不变', () => {
    expect(strong('**中文**，后面')).toEqual(['**中文**']);
    expect(strong('这是**粗体**文字')).toEqual(['**粗体**']);
    expect(strong('a **b** c')).toEqual(['**b**']);
  });
  it('闭合符前是中文标点、后面紧跟中文', () => {
    expect(strong('**中文，**后面')).toEqual(['**中文，**']);
  });
  it('开符后是中文标点、前面紧跟中文', () => {
    expect(strong('前面**「引用」**后面')).toEqual(['**「引用」**']);
    expect(strong('他说**“你好”**然后')).toEqual(['**“你好”**']);
  });
  it('只放宽 CJK，ASCII 行为与 CommonMark 相同', () => {
    expect(strong('a**"b"**c')).toEqual([]);
    expect(strong('**a **b')).toEqual([]);
  });
  it('flanking 纯函数', () => {
    expect(flanking('，', '后', '*', false).canClose).toBe(false);
    expect(cjkFlanking('，', '后', '*').canClose).toBe(true);
    expect(cjkFlanking('a', ' ', '*')).toEqual(flanking('a', ' ', '*', false));
  });
});

describe('GFM 与砍掉的语法', () => {
  it('表格、任务、删除线照常', () => {
    const s = stateOf('| a | b |\n|---|---|\n| 1 | 2 |\n\n- [x] 完成\n\n~~删~~');
    expect(nodes(s, 'Table')).toHaveLength(1);
    expect(nodes(s, 'TaskMarker')).toEqual(['[x]']);
    expect(nodes(s, 'Strikethrough')).toEqual(['~~删~~']);
  });
  it('上下标不解析（W4 已砍）', () => {
    const s = stateOf('H~2~O 与 x^2^');
    expect(nodes(s, 'Subscript')).toEqual([]);
    expect(nodes(s, 'Superscript')).toEqual([]);
  });
});
