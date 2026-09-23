import { describe, expect, it } from 'vitest';
import { parser as commonmark, GFM } from '@lezer/markdown';
import { EditorState } from '@codemirror/state';
import { ensureSyntaxTree } from '@codemirror/language';
import type { Tree } from '@lezer/common';
import { markdownSupport } from '../src/editor/syntax';
import { allExamples, commonmarkExamples, gfmExamples, label } from './spec';

/**
 * 用 CommonMark / GFM 官方用例检查自有扩展没有改变标准语法的解析：
 * 参照是 @lezer/markdown 自带的 CommonMark + GFM 解析器（它本身按这两份规范测试），
 * 我们的配置只允许在自有扩展会生效的用例上与参照不同（`$` 公式、`==` 高亮、`:emoji:`、CJK 强调边界、文首 front matter）。
 */
const reference = commonmark.configure([GFM]);

function dump(tree: Tree): string {
  const out: string[] = [];
  tree.iterate({ enter: n => { if (!n.type.isTop) out.push(`${n.name}@${n.from}-${n.to}`); } });
  return out.join(' ');
}

function ours(md: string): Tree {
  const state = EditorState.create({ doc: md, extensions: [markdownSupport()] });
  return ensureSyntaxTree(state, state.doc.length, 1e9)!;
}

const EXPLAINED = /\$|==|:[a-z0-9_+-]+:|[　-鿿＀-￯]|^---/m;

describe('规范用例：自有扩展不改变标准语法', () => {
  it(`读到了全部用例（CommonMark ${commonmarkExamples.length} 个，GFM ${gfmExamples.length} 个）`, () => {
    expect(commonmarkExamples.length).toBeGreaterThan(600);
    expect(gfmExamples.length).toBeGreaterThan(600);
    expect(gfmExamples.filter(e => e.extension).length).toBeGreaterThan(20);
  });

  it('与参照解析器不同的用例都能由自有扩展解释', () => {
    const differ: string[] = [];
    const unexplained: string[] = [];
    for (const e of allExamples) {
      if (dump(reference.parse(e.markdown)) === dump(ours(e.markdown))) continue;
      differ.push(label(e));
      if (!EXPLAINED.test(e.markdown)) unexplained.push(`${label(e)}\n${JSON.stringify(e.markdown)}`);
    }
    expect(unexplained).toEqual([]);
    // 目前只有 4 个用例不同，都是以 `---` 开头的 Setext 标题用例（CommonMark #96、#98，GFM #66、#68），
    // 文首的 `---…---` 按 front matter 解析，是有意的取舍；数量变化时需要复核
    expect(differ).toEqual(['commonmark #96 Setext headings', 'commonmark #98 Setext headings', 'gfm #66 Setext headings', 'gfm #68 Setext headings']);
  });
});
