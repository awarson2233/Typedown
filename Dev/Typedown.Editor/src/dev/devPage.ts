import '../styles/editor.css';
import './dev.css';
import { EditorState } from '@codemirror/state';
import { EditorView } from '@codemirror/view';
import { ensureSyntaxTree, syntaxTree } from '@codemirror/language';
import { createEditor, coreExtensions } from '../editor/createEditor';
import { markdownSupport, type SyntaxOptions } from '../editor/syntax';
import { codeLanguages } from '../editor/syntax/codeLanguages';
import { parsedLength } from '../editor/state/parseProgress';
import { blockField, blockFieldStats } from '../editor/widgets/blockField';
import { applyTheme } from '../host/theme';
import { sampleDoc } from './sampleDocs';

/**
 * 不依赖宿主的测试页：直接载入示例文档，供手测（输入法、显形）与 Tools/perf-probe 的无头测量。
 * 探针接口挂在 window.__probe / window.__td 上。
 */

interface Probe {
  t: Record<string, number>;
  /** [输入事件时刻, 两帧 rAF 后时刻, 事务 JS 耗时] */
  keys: [number, number, number][];
  longTasks: [number, number][];
  errors: string[];
}
const P: Probe = { t: {}, keys: [], longTasks: [], errors: [] };
(window as unknown as { __probe: Probe }).__probe = P;
try {
  new PerformanceObserver(l => l.getEntries().forEach(e => P.longTasks.push([Math.round(e.startTime), Math.round(e.duration)])))
    .observe({ type: 'longtask', buffered: true });
} catch { /* 不支持 longtask 时忽略 */ }
addEventListener('error', e => P.errors.push(String(e.message)));
addEventListener('unhandledrejection', e => P.errors.push('rej:' + String((e as PromiseRejectionEvent).reason)));

const params = new URLSearchParams(location.search);
const docName = params.get('doc') ?? 'ime';
const docSelect = document.getElementById('dev-doc') as HTMLSelectElement;
docSelect.value = docName;
docSelect.addEventListener('change', () => { params.set('doc', docSelect.value); location.search = params.toString(); });

const dark = document.getElementById('dev-dark') as HTMLInputElement;
dark.checked = params.get('theme') === 'dark';
applyTheme(dark.checked ? 'dark' : 'light');
dark.addEventListener('change', () => applyTheme(dark.checked ? 'dark' : 'light'));

const original = sampleDoc(docName);
function fmOption(v: string | null): 'native' | 'yaml' | false { return v === 'yaml' ? 'yaml' : v === '0' ? false : 'native'; }
const status = document.getElementById('dev-status')!;

// 按键到重绘：beforeinput（捕获阶段）记起点，事务里记 JS 结束，两帧 rAF 后记绘制完成
let inputAt = 0;
let txStart = 0;
const timing = EditorView.updateListener.of(u => {
  if (!u.docChanged || !inputAt) return;
  const at = inputAt, js = performance.now() - txStart;
  inputAt = 0;
  requestAnimationFrame(() => requestAnimationFrame(() => {
    P.keys.push([at, performance.now(), js]);
    showStatus();
  }));
});

P.t.start = performance.now();
const editor = createEditor({
  doc: original,
  parent: document.getElementById('root')!,
  sourceMode: params.get('source') === '1',
  frontmatter: fmOption(params.get('fm')),
  nestedCode: params.get('nested') === '1',
  extensions: [timing],
});
const view = editor.view;
P.t.constructed = performance.now();
view.contentDOM.addEventListener('beforeinput', () => { inputAt = performance.now(); txStart = inputAt; }, true);
requestAnimationFrame(() => requestAnimationFrame(() => { P.t.painted = performance.now(); showStatus(); }));

const source = document.getElementById('dev-source') as HTMLInputElement;
source.checked = params.get('source') === '1';
source.addEventListener('change', () => editor.setSourceMode(source.checked));

document.getElementById('dev-verify')!.addEventListener('click', () => {
  const now = view.state.doc.toString();
  if (now === original) { status.textContent = '字节核对：与载入文本完全相同'; return; }
  let i = 0;
  while (i < now.length && now[i] === original[i]) i++;
  status.textContent = `字节核对：不同，首个差异在偏移 ${i}（已编辑时属正常）`;
});

function showStatus() {
  const k = P.keys[P.keys.length - 1];
  const sel = view.state.selection.main;
  status.textContent = `${docName} ${view.state.doc.length} 字符 · 已解析 ${parsedLength(view.state)} · 光标 ${sel.head}` +
    (k ? ` · 上次按键到重绘 ${Math.round(k[1] - k[0])} ms` : '') + (view.composing ? ' · 组字中' : '');
}
view.dom.addEventListener('keyup', showStatus);
view.dom.addEventListener('compositionend', () => setTimeout(showStatus, 30));

/** 供探针调用：在全新 EditorState 上测 Lezer 全量解析（与视图无关） */
const PARSE_VARIANTS: Record<string, SyntaxOptions> = {
  native: {},
  yaml: { frontmatter: 'yaml' },
  nested: { codeLanguages },
  'yaml+nested': { frontmatter: 'yaml', codeLanguages },
};
function measureFullParse(variant = 'native'): { ms: number; len: number; treeLen: number } {
  const state = EditorState.create({ doc: original, extensions: [markdownSupport(PARSE_VARIANTS[variant])] });
  const t0 = performance.now();
  const tree = ensureSyntaxTree(state, state.doc.length, 1e9);
  return { ms: performance.now() - t0, len: state.doc.length, treeLen: tree?.length ?? -1 };
}

/**
 * 在 EditorState 上测块组件的全量补扫：语法树一次补到文末后，下一个事务里 blockField 把新覆盖的整段扫一遍
 * （即后台解析一次追上全文时 StateField 的开销上界）。
 */
function measureBlockScan(): { ms: number; blocks: number } {
  const s1 = EditorState.create({ doc: original, extensions: [coreExtensions(), blockField] });
  ensureSyntaxTree(s1, s1.doc.length, 1e9);
  const t0 = performance.now();
  const s2 = s1.update({}).state;
  s2.field(blockField);
  const ms = performance.now() - t0;
  let blocks = 0;
  s2.field(blockField).decos.between(0, s2.doc.length, () => { blocks++; });
  return { ms, blocks };
}

function countBlocks(): number {
  let n = 0;
  view.state.field(blockField, false)?.decos.between(0, view.state.doc.length, () => { n++; });
  return n;
}

(window as unknown as Record<string, unknown>).__view = view;
(window as unknown as Record<string, unknown>).__td = {
  editor, original, docName,
  parsedLength: () => parsedLength(view.state),
  treeLength: () => syntaxTree(view.state).length,
  countBlocks,
  blockFieldStats,
  /** 把语法树一次补到文末（模拟「大纲等功能要求全文语法树」），之后每次按键的增量解析都要重走到文末 */
  forceFullParse: () => {
    const t0 = performance.now();
    ensureSyntaxTree(view.state, view.state.doc.length, 60000);
    view.dispatch({});
    return performance.now() - t0;
  },
  measureFullParse,
  measureBlockScan,
  domNodes: () => document.getElementsByTagName('*').length,
};
