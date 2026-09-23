import { EditorState, type Extension } from '@codemirror/state';
import { ensureSyntaxTree } from '@codemirror/language';
import type { Tree } from '@lezer/common';
import { markdownSupport, type SyntaxOptions } from '../src/editor/syntax';

/** 建一个语法树已完整解析的状态 */
export function stateOf(doc: string, extensions: Extension[] = [], syntax: SyntaxOptions = {}): EditorState {
  let state = EditorState.create({ doc, extensions: [markdownSupport(syntax), ...extensions] });
  ensureSyntaxTree(state, state.doc.length, 1e9);
  state = state.update({}).state; // 让 LanguageState 取到补全的树
  return state;
}

export function fullTree(state: EditorState): Tree {
  return ensureSyntaxTree(state, state.doc.length, 1e9)!;
}

/** 语法树里给定名字的节点及其源码 */
export function nodes(state: EditorState, name: string): string[] {
  const out: string[] = [];
  fullTree(state).iterate({ enter: n => { if (n.name === name) out.push(state.sliceDoc(n.from, n.to)); } });
  return out;
}

/** 可重复的伪随机数（mulberry32） */
export function rng(seed: number) {
  return () => {
    seed |= 0; seed = (seed + 0x6d2b79f5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
