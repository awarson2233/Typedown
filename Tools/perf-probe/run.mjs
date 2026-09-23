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
//          inp       G1 口径：真实按键（Input.dispatchKeyEvent），Event Timing 的交互时长（按键到下一次绘制），
//                    顶部 / 中部 / 末尾各 --keys 键；同时录 devtools.timeline 追踪取未取整的值与输入延迟 / 处理 / 呈现拆分；
//                    每轮开测前记整机 CPU 占用，超过 --idle（默认 15%）先等
//          patchcheck  解析补丁的视图级核对：滚到未解析区、Ctrl+End、大段粘贴、撤销，每步核对块组件 ≡ 全量扫描、视口里没露出 `**`
//   --docs=small,mid,rich,large,large-rich   --runs=3   --port=9360   --out=<目录>（默认系统临时目录）
//   --dist=<目录>   换一份 bench 产物（默认 Dev/Typedown.Editor/dist-bench），做补丁前后对照   --keys=20   --idle=15
//   --query=nested=1&fm=yaml   附加到 dev.html 的参数（nested=1 开 parseMixed 嵌套代码语言，fm=yaml 用 lang-yaml 的 front matter）
// 原始结果写成 JSONL 到 --out，不入库。
import { mkdirSync, appendFileSync, writeFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { join, dirname, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { launch, sleep } from './cdp.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const args = Object.fromEntries(process.argv.slice(3).map(a => { const s = a.replace(/^--/, ''), i = s.indexOf('='); return i < 0 ? [s, '1'] : [s.slice(0, i), s.slice(i + 1)]; }));
// --dist 指向另一份 bench 产物（例如打补丁前构建后拷出去的一份），做前后对照
const dist = resolve(args.dist ?? resolve(here, '../../Dev/Typedown.Editor/dist-bench'));
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

/**
 * 整机 CPU 占用（WMI 的格式化计数器，不受系统语言影响）：采样 3 次取中位数（采样进程自己启动时会冲高一次），
 * 附占用最高的 5 个进程（按逻辑核数折算成整机百分比）。
 * 测量要在其他构建 / agent 没占 CPU 时做，所以每轮开测前记一次，超过阈值先等。
 */
function cpuLoad() {
  const ps = `$n=(Get-CimInstance Win32_ComputerSystem).NumberOfLogicalProcessors; $s=@(); 1..3 | % { $s += (Get-CimInstance Win32_PerfFormattedData_PerfOS_Processor -Filter "Name='_Total'").PercentProcessorTime; Start-Sleep -Milliseconds 700 };
$top = Get-CimInstance Win32_PerfFormattedData_PerfProc_Process | ? { $_.Name -notin '_Total','Idle' } | Sort-Object PercentProcessorTime -Descending | Select-Object -First 5 | % { @{ name = $_.Name; pct = [math]::Round($_.PercentProcessorTime / $n, 1) } };
@{ total = ($s | Sort-Object)[1]; samples = $s; top = @($top) } | ConvertTo-Json -Compress -Depth 4`;
  const r = spawnSync('pwsh', ['-NoProfile', '-Command', ps], { encoding: 'utf8' });
  try { return JSON.parse(r.stdout.trim()); } catch { return { error: r.stderr || r.stdout }; }
}

async function waitIdle(threshold = Number(args.idle ?? 15), maxMs = 10 * 60000) {
  const t0 = Date.now();
  for (;;) {
    const load = cpuLoad();
    if (!(load.total > threshold) || Date.now() - t0 > maxMs) return { ...load, waitedMs: Date.now() - t0 };
    console.log(`CPU ${load.total}% > ${threshold}%，等待空闲…`, JSON.stringify(load.top));
    await sleep(15000);
  }
}

const KEY_A = { key: 'a', code: 'KeyA', windowsVirtualKeyCode: 65, nativeVirtualKeyCode: 65 };
/** 真实按键事件（keydown/keypress/beforeinput/input/keyup），Event Timing 只统计这类交互；Input.insertText 不产生按键事件 */
async function pressKey(b) {
  await b.send('Input.dispatchKeyEvent', { type: 'keyDown', ...KEY_A, text: 'a', unmodifiedText: 'a' });
  await sleep(30);
  await b.send('Input.dispatchKeyEvent', { type: 'keyUp', ...KEY_A });
}

const pct = (a, p) => { const s = [...a].sort((x, y) => x - y); return s.length ? s[Math.min(s.length - 1, Math.ceil(p * s.length) - 1)] : null; };

/**
 * 按键到下一次绘制，按 Event Timing 口径（INP 的交互时长）：
 * - observer：PerformanceObserver 'event'（durationThreshold 最低 16，duration 按 8 ms 取整）。同一 interactionId 的条目取最大 duration；
 *   低于 16 ms 的按键没有条目，记作 "<16"。
 * - trace：同一时刻的 devtools.timeline 里的 EventTiming 追踪事件，是同一个指标的未取整值（含低于 16 ms 的按键），
 *   并拆成输入延迟（processingStart − startTime）、处理（processingEnd − processingStart）、呈现延迟（其余）。
 * 文档 large-rich（1 MB，带表格 / 公式 / mermaid 块组件），顶部 / 中部 / 末尾各输入 --keys 个字符（默认 20），每键间隔约 180 ms。
 */
async function inp(doc, run) {
  const R = { mode, doc, run, dist, query: args.query ?? '' };
  R.cpuBefore = await waitIdle();
  const { b } = await open(doc);
  const trace = [];
  b.on(m => { if (m.method === 'Tracing.dataCollected') trace.push(...m.params.value); });
  try {
    await b.send('Emulation.setFocusEmulationEnabled', { enabled: true });
    await sleep(1500);
    R.len = await b.evalJs('__view.state.doc.length');
    R.outline = await b.evalJs('__td.outlineStats()');
    R.outlineScanMs = +(await b.evalJs('__td.measureOutlineScan()')).ms.toFixed(1);
    const nKeys = Number(args.keys ?? 20);
    const positions = {
      top: `__td.original.indexOf(${JSON.stringify(MARKER)}) + ${MARKER.length}`,
      mid: `(() => { const d = __view.state.doc.toString(), i = d.indexOf('times.', d.length >> 1); return i + 6; })()`,
      end: `(() => { const d = __view.state.doc.toString(), i = d.lastIndexOf('times.'); return i + 6; })()`,
    };
    R.positions = {};
    for (const [where, expr] of Object.entries(positions)) {
      await placeCaret(b, expr, 2500);
      const P = { parsedBefore: await b.evalJs('__td.parsedLength()') };
      const base = await b.evalJs('JSON.stringify({ e: __probe.events.length, k: __probe.keydowns.length, j: __probe.keys.length })').then(JSON.parse);
      trace.length = 0;
      await b.send('Tracing.start', { traceConfig: { includedCategories: ['devtools.timeline'] }, transferMode: 'ReportEvents' });
      const done = new Promise(r => b.on(m => { if (m.method === 'Tracing.tracingComplete') r(); }));
      for (let k = 0; k < nKeys; k++) { await pressKey(b); await sleep(150); }
      await sleep(600);
      await b.send('Tracing.end');
      await Promise.race([done, sleep(20000)]);
      const got = JSON.parse(await b.evalJs(`JSON.stringify({ e: __probe.events.slice(${base.e}), k: __probe.keydowns.slice(${base.k}), j: __probe.keys.slice(${base.j}).map(x => x[2]) })`));
      // observer：按 interactionId 取每次交互的最大 duration
      const byId = new Map();
      for (const [type, start, dur, ps, pe, id] of got.e) if (id) byId.set(id, Math.max(byId.get(id) ?? 0, dur));
      const observed = [...byId.values()];
      P.keys = got.k.length;
      P.observer = { entries: observed.length, below16: got.k.length - observed.length, durations: observed.sort((a, b) => a - b) };
      // 未见条目的按键按 16 计（上界），据此给中位数与 p90 的上界
      const padded = [...observed, ...Array(Math.max(0, got.k.length - observed.length)).fill(16)];
      P.observer.medianUpper = pct(padded, 0.5);
      P.observer.p90Upper = pct(padded, 0.9);
      // trace：EventTiming 追踪事件的 data 有 type、duration、timeStamp、processingStart/End、interactionId。
      // 一次按键 = keydown（带 interactionId）+ 同一 timeStamp 的 keypress/beforeinput/input（CM6 的 DOM 观察器与事务在这里跑）+ 之后的 keyup。
      const ev = trace.filter(t => t.name === 'EventTiming' && t.args?.data?.type).map(t => t.args.data);
      const seen = new Set();
      const uniq = ev.filter(d => { const k = `${d.type}@${d.timeStamp}`; if (seen.has(k)) return false; seen.add(k); return true; });
      const keyups = new Map(uniq.filter(d => d.type === 'keyup' && d.interactionId).map(d => [d.interactionId, d]));
      P.trace = uniq.filter(d => d.type === 'keydown' && d.interactionId).map(kd => {
        const group = uniq.filter(d => Math.abs(d.timeStamp - kd.timeStamp) < 0.01 && d.type !== 'keyup');
        const lastEnd = Math.max(...group.map(d => d.processingEnd));
        const up = keyups.get(kd.interactionId);
        return {
          dur: +Math.max(kd.duration, up?.duration ?? 0).toFixed(2), // INP 口径：同一交互取 keydown / keyup 的较大者
          keydown: +kd.duration.toFixed(2),
          delay: +(kd.processingStart - kd.timeStamp).toFixed(2),
          proc: +group.reduce((s, d) => s + (d.processingEnd - d.processingStart), 0).toFixed(2),
          present: +(kd.timeStamp + kd.duration - lastEnd).toFixed(2),
        };
      });
      const col = k => P.trace.map(x => x[k]);
      P.traceSummary = P.trace.length ? { n: P.trace.length, median: pct(col('dur'), 0.5), p90: pct(col('dur'), 0.9), max: Math.max(...col('dur')),
        delayMedian: pct(col('delay'), 0.5), procMedian: pct(col('proc'), 0.5), procP90: pct(col('proc'), 0.9), presentMedian: pct(col('present'), 0.5), presentP90: pct(col('present'), 0.9) } : null;
      P.jsMedian = +pct(got.j, 0.5)?.toFixed(2);
      P.parsedAfter = await b.evalJs('__td.parsedLength()');
      R.positions[where] = P;
    }
    R.errors = await b.evalJs('__probe.errors');
  } finally { await b.close(); }
  log(R);
}

/**
 * 解析补丁在视图里的行为：开头打字后，
 * 滚到未解析区、Ctrl+End 跳到文末、大段粘贴、Ctrl+Z 撤销，每步之后等语法树覆盖视口（记等了多久），
 * 再核对块组件 ≡ 全量扫描、视口里没有露出的 `**`、正文字节。
 */
async function patchcheck(doc) {
  const { b } = await open(doc);
  const R = { mode, doc, dist, steps: [] };
  const settle = async (name, extra = {}) => {
    const t0 = Date.now();
    let s;
    for (;;) {
      s = await b.evalJs('JSON.stringify({ parsed: __td.parsedLength(), vpTo: __view.viewport.to })').then(JSON.parse);
      if (s.parsed >= s.vpTo || Date.now() - t0 > 10000) break;
      await sleep(20);
    }
    const coveredAfterMs = Date.now() - t0;
    await sleep(300);
    R.steps.push({ name, coveredAfterMs, ...s, ...JSON.parse(await b.evalJs('JSON.stringify(__td.checkBlocks())')), stars: await b.evalJs('__td.visibleStars()'), ...extra });
  };
  const key = async (key, code, vk, modifiers = 0) => {
    await b.send('Input.dispatchKeyEvent', { type: 'rawKeyDown', key, code, windowsVirtualKeyCode: vk, nativeVirtualKeyCode: vk, modifiers });
    await b.send('Input.dispatchKeyEvent', { type: 'keyUp', key, code, windowsVirtualKeyCode: vk, nativeVirtualKeyCode: vk, modifiers });
  };
  try {
    await b.send('Emulation.setFocusEmulationEnabled', { enabled: true });
    await sleep(1500);
    await placeCaret(b, `__td.original.indexOf(${JSON.stringify(MARKER)}) + ${MARKER.length}`, 1500);
    for (let k = 0; k < 3; k++) { await pressKey(b); await sleep(120); }
    R.parsedRightAfterTyping = await b.evalJs('__td.parsedLength()');
    await settle('打字后');
    // 1. 选区不动，直接把滚动条拖到一半：视口落在同步解析没碰过的区域
    // 编辑器自己滚动时滚 scrollDOM，页面整体滚动时滚 window（dev.html 两种布局都可能）
    await b.evalJs('(() => { const s = __view.scrollDOM; if (s.scrollHeight > s.clientHeight + 10) s.scrollTop = s.scrollHeight / 2; else window.scrollTo(0, document.documentElement.scrollHeight / 2); })()');
    await sleep(100);
    R.parsedRightAfterScroll = await b.evalJs('JSON.stringify({ parsed: __td.parsedLength(), vpTo: __view.viewport.to })').then(JSON.parse);
    await settle('滚到未解析区');
    // 2. Ctrl+End
    await b.evalJs('__view.focus()');
    await key('End', 'End', 35, 2);
    await settle('Ctrl+End 跳到文末', { head: await b.evalJs('__view.state.selection.main.head'), len: await b.evalJs('__view.state.doc.length') });
    // 3. 在文末粘贴 20 万字符（与粘贴走同一条事务路径：userEvent input.paste）
    const before = await b.evalJs('__view.state.doc.length');
    const pasteMs = await b.evalJs(`(() => { const v = __view, t = __td.original.slice(0, 200000), h = v.state.selection.main.head, t0 = performance.now(); v.dispatch({ changes: { from: h, insert: t }, selection: { anchor: h + t.length }, userEvent: 'input.paste', scrollIntoView: true }); return performance.now() - t0; })()`);
    await settle('文末粘贴 20 万字符', { pasteDispatchMs: +pasteMs.toFixed(1), grew: (await b.evalJs('__view.state.doc.length')) - before });
    // 4. Ctrl+Z 撤销粘贴
    await key('z', 'KeyZ', 90, 2);
    await settle('撤销粘贴');
    R.finalText = await b.evalJs(`(() => { const o = __td.original, i = o.indexOf(${JSON.stringify(MARKER)}) + ${MARKER.length}; return __view.state.doc.toString() === o.slice(0, i) + 'aaa' + o.slice(i); })()`);
    R.errors = await b.evalJs('__probe.errors');
  } finally { await b.close(); }
  log(R);
}

console.log(`输出：${outFile}`);
if (mode === 'filemode') await filemode();
else for (const doc of docs) {
  if (mode === 'perf') for (let r = 0; r < runs; r++) await perf(doc, r);
  else if (mode === 'inp') for (let r = 0; r < runs; r++) await inp(doc, r);
  else if (mode === 'patchcheck') await patchcheck(doc);
  else if (mode === 'coverage') await coverage(doc);
  else if (mode === 'fidelity') await fidelity(doc);
  else if (mode === 'smoke') await smoke(doc);
}
