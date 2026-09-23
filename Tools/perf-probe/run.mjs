// Typedown 新编辑引擎（Dev/Typedown.Editor）的无头性能与保真探针。
//
// 前提：在 Dev/Typedown.Editor 下先执行 `yarn build:bench`，产物在 dist-bench/（含 dev.html）。
// 用法：node Tools/perf-probe/run.mjs <mode> [选项]
//   mode = perf      每个文档起独立的无头 Edge，测首帧、段落末尾连续 6 键的按键到两帧 rAF、
//                    Lezer 全量解析、块组件全量补扫、堆；large 系列另测文档中部的按键
//          coverage  打开后静置，按时间点记录默认后台解析覆盖到的长度与块组件数
//          fidelity  载入后立即取回 ≡ 原文；段落末尾输入 6 个字符后 ≡ 原文在该处插入
//          smoke     载入各文档，收集页面错误与块组件数，保存截图
//          filemode  用 file:// 打开 dev.html：分别不带参数与带宿主的 WebView2 启动参数，看 ESM 模块能否加载
//   --docs=small,mid,rich,large,large-rich   --runs=3   --port=9360   --out=<目录>（默认系统临时目录）
//   --query=nested=1&fm=yaml   附加到 dev.html 的参数（nested=1 开 parseMixed 嵌套代码语言，fm=yaml 用 lang-yaml 的 front matter）
// 原始结果写成 JSONL 到 --out，不入库。
import { mkdirSync, appendFileSync, writeFileSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { launch, sleep } from './cdp.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const dist = resolve(here, '../../Dev/Typedown.Editor/dist-bench');
const args = Object.fromEntries(process.argv.slice(3).map(a => { const s = a.replace(/^--/, ''), i = s.indexOf('='); return i < 0 ? [s, '1'] : [s.slice(0, i), s.slice(i + 1)]; }));
const mode = process.argv[2] ?? 'perf';
const docs = (args.docs ?? 'small,mid,rich,large,large-rich').split(',');
const runs = Number(args.runs ?? 3);
let port = Number(args.port ?? 9360);
const out = args.out ?? join(tmpdir(), 'td-perf-probe');
mkdirSync(out, { recursive: true });
const outFile = join(out, `${mode}-${new Date().toISOString().replace(/[:.]/g, '-')}.jsonl`);
// 与 Dev/Typedown.Core/Config.cs 的 WebView2Args 相同
const HOST_FLAGS = ['--disable-web-security', '--allow-file-access-from-files'];
const query = args.query ? '&' + args.query : '';
const pageUrl = doc => pathToFileURL(join(dist, 'dev.html')).href + `?doc=${doc}${query}`;
const MARKER = 'lazy dog 0 times.';

async function open(doc, flags = HOST_FLAGS) {
  const b = await launch(port++, flags);
  await b.send('Page.enable');
  await b.send('Performance.enable');
  const navAt = Date.now();
  await b.send('Page.navigate', { url: pageUrl(doc) });
  let t;
  while (Date.now() - navAt < 60000) {
    await sleep(50);
    t = await b.evalJs('window.__probe && window.__probe.t.painted !== undefined ? JSON.stringify(window.__probe.t) : null');
    if (typeof t === 'string') break;
  }
  if (typeof t !== 'string') { await b.close(); throw new Error(`${doc} 未在 60 s 内完成首帧`); }
  return { b, t: JSON.parse(t) };
}

/** 把光标放到 offset 处、滚动到可见并聚焦，等布局与解析稳定 */
async function placeCaret(b, offsetExpr, settle = 800) {
  await b.evalJs(`(() => { const v = __view; const pos = ${offsetExpr}; v.focus(); v.dispatch({ selection: { anchor: pos }, scrollIntoView: true }); return pos; })()`);
  await sleep(settle);
}

/** 连续输入 n 个字符，返回每键 [按键→两帧 rAF, 事务 JS 耗时] */
async function typeKeys(b, n = 6) {
  const res = [];
  for (let k = 0; k < n; k++) {
    const before = await b.evalJs('__probe.keys.length');
    const t0 = await b.evalJs('performance.now()');
    await b.send('Input.insertText', { text: 'a' });
    let rec;
    for (let i = 0; i < 400 && !rec; i++) {
      await sleep(5);
      rec = await b.evalJs(`__probe.keys.length > ${before} ? __probe.keys[${before}] : null`);
    }
    if (!rec) { res.push(null); continue; }
    const [inputAt, paintAt, js] = rec;
    res.push({ paint: +(paintAt - (inputAt || t0)).toFixed(1), fromCdp: +(paintAt - t0).toFixed(1), js: +js.toFixed(2) });
    await sleep(150);
  }
  return res;
}

const median = a => { const s = [...a].sort((x, y) => x - y); return s.length ? s[Math.floor(s.length / 2)] : null; };
const log = r => { appendFileSync(outFile, JSON.stringify(r) + '\n'); console.log(JSON.stringify(r)); };

async function perf(doc, run) {
  const { b, t } = await open(doc);
  const R = { mode, doc, run, query: args.query ?? '' };
  try {
    R.construct = +(t.constructed - t.start).toFixed(1);
    R.firstPaint = +(t.painted - t.start).toFixed(1);
    R.navToPaint = +t.painted.toFixed(1);
    await sleep(1500);
    R.len = await b.evalJs('__view.state.doc.length');
    R.parsedAtLoad = await b.evalJs('__td.parsedLength()');
    R.blocksAtLoad = await b.evalJs('__td.countBlocks()');
    R.longTasksLoad = await b.evalJs('JSON.stringify(__probe.longTasks)');
    await placeCaret(b, `__td.original.indexOf(${JSON.stringify(MARKER)}) + ${MARKER.length}`);
    R.keysTop = await typeKeys(b);
    if (doc.startsWith('large')) {
      // 文档中部：第 N/2 处之后的第一个正文段落末尾（先滚过去，等后台解析与块组件补齐）
      await placeCaret(b, `(() => { const o = __td.original, i = o.indexOf('times.', o.length >> 1); return i + 6 + (__view.state.doc.length - o.length); })()`, 2500);
      R.parsedMid = await b.evalJs('__td.parsedLength()');
      R.keysMid = await typeKeys(b);
      // 语法树补到文末后回到文首段落再打字：增量解析要重走到文末，这是按键开销的上界
      R.forceFullParseMs = +(await b.evalJs('__td.forceFullParse()', 120000)).toFixed(1);
      await placeCaret(b, `__view.state.doc.toString().indexOf(${JSON.stringify(MARKER)}) + ${MARKER.length}`, 1500);
      R.parsedFull = await b.evalJs('__td.parsedLength()');
      R.keysFullTree = await typeKeys(b);
    }
    R.errors = await b.evalJs('__probe.errors');
    const m = Object.fromEntries((await b.send('Performance.getMetrics')).result.metrics.map(x => [x.name, x.value]));
    R.heapMB = Math.round(m.JSHeapUsedSize / 1048576);
    R.domNodes = await b.evalJs('__td.domNodes()');
    if (run === 0) {
      // Lezer 全量解析：native = 默认配置；yaml = lang-yaml 包一层；nested = lang-markdown 的 parseCode 嵌套代码语言
      const variants = ['native', 'yaml', 'nested', 'yaml+nested'];
      const parse = Object.fromEntries(variants.map(v => [v, []]));
      const scan = [];
      for (let i = 0; i < 3; i++) {
        for (const v of variants) parse[v].push(await b.evalJs(`__td.measureFullParse(${JSON.stringify(v)})`, 120000));
        scan.push(await b.evalJs('__td.measureBlockScan()', 120000));
      }
      R.fullParseMs = Object.fromEntries(variants.map(v => [v, parse[v].map(p => +p.ms.toFixed(1))]));
      R.fullParseTreeLen = parse.native[0].treeLen;
      R.blockScanMs = scan.map(s => +s.ms.toFixed(1));
      R.blockScanBlocks = scan[0].blocks;
    }
  } finally { await b.close(); }
  const sum = keys => keys && { median: median(keys.filter(Boolean).map(k => k.paint)), max: Math.max(...keys.filter(Boolean).map(k => k.paint)), jsMedian: median(keys.filter(Boolean).map(k => k.js)) };
  R.keysTopSummary = sum(R.keysTop);
  if (R.keysMid) R.keysMidSummary = sum(R.keysMid);
  if (R.keysFullTree) R.keysFullTreeSummary = sum(R.keysFullTree);
  log(R);
}

async function coverage(doc) {
  const { b } = await open(doc);
  const R = { mode, doc, points: [] };
  try {
    const start = Date.now();
    for (const at of [0, 2000, 5000, 10000, 20000, 35000, 65000]) {
      await sleep(Math.max(0, at - (Date.now() - start)));
      R.points.push({ ms: at, parsed: await b.evalJs('__td.parsedLength()'), blocks: await b.evalJs('__td.countBlocks()') });
    }
    R.len = await b.evalJs('__view.state.doc.length');
  } finally { await b.close(); }
  log(R);
}

async function fidelity(doc) {
  const { b } = await open(doc);
  const R = { mode, doc };
  try {
    await sleep(1000);
    R.loadIdentical = await b.evalJs('__view.state.doc.toString() === __td.original');
    // 滚一遍全文，让所有块组件与行内装饰都真正渲染过一次，再核对
    await b.evalJs(`(async () => { const v = __view; for (let p = 0; p < v.state.doc.length; p += 20000) { v.dispatch({ effects: [], selection: { anchor: p }, scrollIntoView: true }); await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r))); } })()`, 300000);
    R.afterScrollIdentical = await b.evalJs('__view.state.doc.toString() === __td.original');
    await placeCaret(b, `__td.original.indexOf(${JSON.stringify(MARKER)}) + ${MARKER.length}`);
    await typeKeys(b);
    R.editOnlyThere = await b.evalJs(`(() => { const o = __td.original, i = o.indexOf(${JSON.stringify(MARKER)}) + ${MARKER.length}; return __view.state.doc.toString() === o.slice(0, i) + 'aaaaaa' + o.slice(i); })()`);
    R.errors = await b.evalJs('__probe.errors');
  } finally { await b.close(); }
  log(R);
}

async function smoke(doc) {
  const { b, t } = await open(doc);
  const R = { mode, doc, firstPaint: +(t.painted - t.start).toFixed(1) };
  try {
    await sleep(3000);
    R.blocks = await b.evalJs('__td.countBlocks()');
    R.widgets = await b.evalJs(`({ table: document.querySelectorAll('.cm-td-table').length, math: document.querySelectorAll('.cm-td-math .katex').length, mermaid: document.querySelectorAll('.cm-td-mermaid svg').length, bullets: document.querySelectorAll('.cm-td-bullet').length, tasks: document.querySelectorAll('.cm-td-task').length, inlineMath: document.querySelectorAll('.cm-td-math-inline .katex').length })`);
    R.visibleStars = await b.evalJs(`(document.querySelector('.cm-content').innerText.match(/\\*\\*/g) || []).length`);
    R.errors = await b.evalJs('__probe.errors');
    const shot = await b.send('Page.captureScreenshot', { format: 'png' });
    if (shot.result) writeFileSync(join(out, `smoke-${doc}.png`), Buffer.from(shot.result.data, 'base64'));
  } finally { await b.close(); }
  log(R);
}

async function filemode() {
  for (const [name, flags] of [['no-flags', []], ['host-flags', HOST_FLAGS]]) {
    const b = await launch(port++, flags);
    const R = { mode, variant: name };
    try {
      await b.send('Page.enable');
      await b.send('Page.navigate', { url: pageUrl('ime') });
      await sleep(5000);
      R.moduleRan = await b.evalJs('!!(window.__probe && window.__probe.t.painted !== undefined)');
      // 懒块能否经 import() 从 file:// 加载：ime 文档里有行内公式、公式块与 mermaid
      R.katexRendered = await b.evalJs(`document.querySelectorAll('.katex').length`);
      R.mermaidRendered = await b.evalJs(`document.querySelectorAll('.cm-td-mermaid svg').length`);
    } finally { await b.close(); }
    log(R);
  }
}

console.log(`输出：${outFile}`);
if (mode === 'filemode') await filemode();
else for (const doc of docs) {
  if (mode === 'perf') for (let r = 0; r < runs; r++) await perf(doc, r);
  else if (mode === 'coverage') await coverage(doc);
  else if (mode === 'fidelity') await fidelity(doc);
  else if (mode === 'smoke') await smoke(doc);
}
