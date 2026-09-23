import { describe, expect, it } from 'vitest';
import { nodes, stateOf } from './helpers';

describe('脚注', () => {
  it('行内引用', () => {
    const s = stateOf('正文[^1]与[^note]，[^ 不是]、[^]也不是');
    expect(nodes(s, 'FootnoteReference')).toEqual(['[^1]', '[^note]']);
    expect(nodes(s, 'FootnoteLabel')).toEqual(['1', 'note']);
  });
  it('定义：到空行为止，内容有行内节点', () => {
    const s = stateOf('正文[^1]\n\n[^1]: 脚注**内容**\n续行\n\n后文');
    expect(nodes(s, 'FootnoteDefinition')).toEqual(['[^1]: 脚注**内容**\n续行']);
    expect(nodes(s, 'FootnoteDefinitionMark')).toEqual(['[^', ']:']);
    expect(nodes(s, 'StrongEmphasis')).toEqual(['**内容**']);
    expect(nodes(s, 'LinkReference')).toEqual([]);
  });
  it('相邻的两条定义各自成块', () => {
    const s = stateOf('[^a]: 甲\n[^b]: http://example.com\n');
    expect(nodes(s, 'FootnoteDefinition')).toEqual(['[^a]: 甲', '[^b]: http://example.com']);
  });
  it('普通链接引用定义不受影响', () => {
    expect(nodes(stateOf('[foo]: /url "title"\n\n[foo]'), 'LinkReference')).toEqual(['[foo]: /url "title"']);
  });
  it('引用里的定义', () => {
    expect(nodes(stateOf('> [^x]: 引用里\n> 续行'), 'FootnoteDefinition')).toEqual(['[^x]: 引用里\n> 续行']);
  });
});

describe('[TOC]', () => {
  it('独占一行、大小写不限', () => {
    expect(nodes(stateOf('# 标题\n\n[TOC]\n\n正文'), 'TableOfContents')).toEqual(['[TOC]']);
    expect(nodes(stateOf('[toc]  \n'), 'TableOfContents')).toEqual(['[toc]']);
  });
  it('段落中间、缩进、容器内都不是目录', () => {
    expect(nodes(stateOf('文字\n[TOC]'), 'TableOfContents')).toEqual([]);
    expect(nodes(stateOf('    [TOC]'), 'TableOfContents')).toEqual([]);
    expect(nodes(stateOf('> [TOC]'), 'TableOfContents')).toEqual([]);
    expect(nodes(stateOf('[TOC] 后面有字'), 'TableOfContents')).toEqual([]);
  });
});
