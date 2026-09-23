import '../styles/theme.css';
import '../styles/typography.css';
import '../styles/inline.css';
import '../styles/editor.css';
import './dev.css';
import { EditorState } from '@codemirror/state';
import { EditorView } from '@codemirror/view';
import { ensureSyntaxTree, syntaxTree } from '@codemirror/language';
import { coreExtensions } from '../editor/createEditor';
import { markdownSupport, type SyntaxOptions } from '../editor/syntax';
import { codeLanguages } from '../editor/syntax/codeLanguages';
import { parsedLength } from '../editor/state/parseProgress';
import { blockField, blockFieldStats } from '../editor/widgets/blockField';
import { outlineField, scanOutline } from '../editor/state/outline';
import { startEditorApp } from '../app';
import { FakeHost } from '../bridge/channel';
import { sampleDoc } from './sampleDocs';

/**
 * 不依赖宿主的测试页：经假宿主载入示例文档，供手测（输入法、显形）与 Tools/perf-probe 的无头测量。
 * 探针接口挂在 window.__probe / window.__td / window.__host 上。
 */

interface Probe {
  t: Record<string, number>;
  /** [输入事件时刻, 两帧 rAF 后时刻, 事务 JS 耗时] */
  keys: [number, number, number][];
  /** Event Timing（PerformanceObserver 'event'，durationThreshold 16，duration 按 8 ms 取整）：[type, startTime, duration, processingStart, processingEnd, interactionId] */
  events: [string, number, number, number, number, number][];
  /** 页面收到的 keydown 的 event.timeStamp（低于阈值的交互没有 Event Timing 条目，靠它数总键数） */
  keydowns: number[];
  longTasks: [number, number][];
  errors: string[];
}
const P: Probe = { t: {}, keys: [], events: [], keydowns: [], longTasks: [], errors: [] };
(window as unknown as { __probe: Probe }).__probe = P;
try {
  new PerformanceObserver(l => l.getEntries().forEach(e => {
    const x = e as PerformanceEventTiming;
    P.events.push([x.name, x.startTime, x.duration, x.processingStart, x.processingEnd, x.interactionId ?? 0]);
  })).observe({ type: 'event', durationThreshold: 16, buffered: true } as PerformanceObserverInit);
} catch { /* 不支持 Event Timing 时忽略 */ }
addEventListener('keydown', e => P.keydowns.push(e.timeStamp), true);
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

// 与宿主相同的启动路径（app.ts），只是传输换成假宿主：ready 后由假宿主发 doc.load 装入示例文档，
// 所以探针测到的按键开销包含桥接层（正文同步、选区 / 大纲 / 历史上报、快捷键捕获）。宿主发出的报文记在 __host.received。
const host = new FakeHost();
const THEMES = {
  light: { isDark: false, accent: { r: 9, g: 105, b: 218, a: 1 }, background: { r: 255, g: 255, b: 255, a: 1 } },
  dark: { isDark: true, accent: { r: 74, g: 163, b: 255, a: 1 }, background: { r: 30, g: 30, b: 30, a: 1 } },
};
host.onMessage(m => {
  if (m.k !== 'evt') return;
  if (m.t === 'lifecycle.ready') {
    P.t.ready = performance.now();
    host.command('doc.load', { version: 1, text: original, basePath: '' });
    P.t.constructed = performance.now();
    requestAnimationFrame(() => requestAnimationFrame(() => { P.t.painted = performance.now(); showStatus(); }));
  } else if (m.t === 'doc.rendered') P.t.rendered = performance.now();
});
P.t.start = performance.now();
const app = startEditorApp({
  parent: document.getElementById('root')!,
  transport: host,
  init: { settings: { sourceCode: params.get('source') === '1' }, theme: dark.checked ? THEMES.dark : THEMES.light, keymap: [] },
  editor: { frontmatter: fmOption(params.get('fm')), nestedCode: params.get('nested') === '1' },
  extensions: [timing],
});
const editor = app.editor;
const view = editor.view;
view.contentDOM.addEventListener('beforeinput', () => { inputAt = performance.now(); txStart = inputAt; }, true);
dark.addEventListener('change', () => host.command('view.theme', { theme: dark.checked ? THEMES.dark : THEMES.light }));

const source = document.getElementById('dev-source') as HTMLInputElement;
source.checked = params.get('source') === '1';
source.addEventListener('change', () => host.command('view.settings', { changes: { sourceCode: source.checked } }));

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

/**
 * 块组件核对：当前视图的块装饰在已覆盖范围内，是否与「同一文本 + 同一选区、语法树解析到文末」的全量扫描逐个相同。
 * 给 perf-probe 的 patchcheck 用（同步解析只到视口末尾后，滚动 / 跳转 / 粘贴 / 撤销时块组件是否正确）。
 */
function checkBlocks() {
  const s = view.state;
  let fresh = EditorState.create({ doc: s.doc, selection: s.selection, extensions: [markdownSupport({ frontmatter: fmOption(params.get('fm')) }), blockField] });
  ensureSyntaxTree(fresh, fresh.doc.length, 1e9);
  fresh = fresh.update({}).state;
  const describe = (st: EditorState) => {
    const out: [number, string][] = [];
    st.field(blockField).decos.between(0, st.doc.length, (from, to, d) => {
      const w = d.spec.widget as { src?: string; preview?: boolean; constructor: { name: string } } | undefined;
      out.push([to, `${w?.constructor.name}@${from}-${to}:${w?.preview ? 'preview:' : ''}${w?.src ?? ''}`]);
    });
    return out;
  };
  const covered = s.field(blockField).covered;
  const a = describe(s).filter(x => x[0] <= covered).map(x => x[1]), b = describe(fresh).filter(x => x[0] <= covered).map(x => x[1]);
  let firstDiff = -1;
  for (let i = 0; i < Math.max(a.length, b.length); i++) if (a[i] !== b[i]) { firstDiff = i; break; }
  return { covered, parsed: parsedLength(s), vpFrom: view.viewport.from, vpTo: view.viewport.to, blocks: a.length, same: firstDiff < 0, diff: firstDiff < 0 ? null : [a[firstDiff] ?? null, b[firstDiff] ?? null] };
}

/** 视口里还露在外面的 `**`（光标所在行除外）：语法树没覆盖视口时，行内显形不生效，标记会露出来 */
function visibleStars(): number {
  const head = view.state.doc.lineAt(view.state.selection.main.head);
  let n = 0;
  for (const el of Array.from(view.contentDOM.querySelectorAll('.cm-line'))) {
    const pos = view.posAtDOM(el);
    if (pos >= head.from && pos <= head.to) continue;
    n += ((el.textContent ?? '').match(/\*\*/g) ?? []).length;
  }
  return n;
}

function countBlocks(): number {
  let n = 0;
  view.state.field(blockField, false)?.decos.between(0, view.state.doc.length, () => { n++; });
  return n;
}

(window as unknown as Record<string, unknown>).__view = view;
(window as unknown as Record<string, unknown>).__host = host;
(window as unknown as Record<string, unknown>).__app = app;
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
  checkBlocks,
  visibleStars,
  outlineStats: () => { const o = view.state.field(outlineField); return { headings: o.headings.length, items: o.items.length, revision: o.revision }; },
  /** 行首扫描器全量扫一遍全文的耗时（载入时 outlineField.create 的开销） */
  measureOutlineScan: () => { const t0 = performance.now(); const o = scanOutline(view.state.doc); return { ms: performance.now() - t0, headings: o.headings.length }; },
  domNodes: () => document.getElementsByTagName('*').length,
};
