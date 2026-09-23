// 新编辑引擎（CM6）与旧编辑器（Muya）的排版数值对比：同一视口、同一套宿主设置、同一篇文档，
// 在两个无头 Edge 里分别装载旧页面与新页面，按源码顺序逐块比较位置、尺寸、首行文字的计算样式与实际渲染字体，
// 以及行内元素（粗体、行内代码、链接等）的计算样式。输出简短报告，未达标时退出码为 1。
//
// 前提：
//   新页面：Dev/Typedown.Editor 的 bench 产物（dist-bench/index.html）；默认先执行一次 `yarn build:bench`（--no-build 跳过）。
//   旧页面：旧编辑器的生产产物（CRA 构建的 static/js/main.*.js），默认取主工作树的 Dev/Typedown.WinUI/Resources/Statics，
//           也可用 --old=<目录> 指定。旧页面以 file:// 加上宿主旧的启动参数加载，chrome.webview 由桩应答旧协议的 invoke。
// 用法：node Tools/style-parity/run.mjs [选项]
//   --matrix=full|quick  full（默认）：全部文档 × 视口 1280/900 × 明暗 × 默认字号行高，再加 1280 亮色下的 14/1.4 与 20/2.0；
//                        quick：全部文档 × 1280 × 亮色 × 默认字号行高
//   --docs=showcase,blocks,ime,docs/architecture.md,...   默认全部语料（dev 页样例、仓库 docs/*.md、旧编辑器 README）
//   --out=<目录>         报告与明细（report.md、detail.json），默认系统临时目录下的 td-style-parity
//   --shots=<目录>       另存 1280 宽、默认字号下 showcase 的新旧、明暗截图
//   --old=<目录> --new=<目录> --no-build --port=9470 --verbose
// 判据（未列入 KNOWN 的差异都算不达标）：全文末尾块顶部的累计漂移 ≤ 2px，单块高度差 ≤ 1px，
// 内容区 left / width 差 ≤ 1px，首行文字的字体栈、实际渲染字体、字号、字重、字形、行高、字距、颜色一致，行内元素样式一致。
import { mkdirSync, writeFileSync, readFileSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';
import { OldPage, NewPage, FONT_DEFAULT, loadCorpus, withSentinel, writeFixture, findOldStatics, buildNew, editorDir } from './pages.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const args = Object.fromEntries(process.argv.slice(2).map(a => { const s = a.replace(/^--/, ''), i = s.indexOf('='); return i < 0 ? [s, '1'] : [s.slice(0, i), s.slice(i + 1)]; }));
const out = resolve(args.out ?? join(tmpdir(), 'td-style-parity'));
mkdirSync(out, { recursive: true });
const verbose = !!args.verbose;
const port = Number(args.port ?? 9470);

const corpus = await loadCorpus();
const docNames = args.docs ? args.docs.split(',') : [...corpus.keys()];
const basePath = writeFixture(join(out, 'fixture'));

// ── 配置矩阵 ──
const matrix = [];
if (args.matrix === 'quick') matrix.push({ width: 1280, theme: 'light', font: FONT_DEFAULT });
else {
  for (const width of [1280, 900]) for (const theme of ['light', 'dark']) matrix.push({ width, theme, font: FONT_DEFAULT });
  matrix.push({ width: 1280, theme: 'light', font: { fontSize: 14, lineHeight: 1.4 } });
  matrix.push({ width: 1280, theme: 'light', font: { fontSize: 20, lineHeight: 2.0 } });
}
const cfgName = c => `${c.width}/${c.theme}/${c.font.fontSize}·${c.font.lineHeight}`;

// ── 产物 ──
const newDist = resolve(args.new ?? join(editorDir, 'dist-bench'));
if (!args['no-build'] && !args.new) buildNew();
const oldStatics = findOldStatics(args.old);
if (!oldStatics) { console.error('找不到旧编辑器产物（CRA 构建的 static/js/main.*.js），用 --old=<目录> 指定'); process.exit(2); }

// ── 对齐与比较 ──

const BOX_TYPES = t => !/(paragraph|list-item|task-item|heading-\d|hr|image|link-ref)$/.test(t);
/** 首行文字相同；一边渲染了另一边没渲染的行内标记（如 ==高亮==）时文字会在中途分岔，前 8 个字相同也算 */
const commonPrefix = (a, b) => { let i = 0; while (i < a.length && i < b.length && a[i] === b[i]) i++; return i; };
const sameKey = (a, b) => a === b || (Math.min(a.length, b.length) >= 4 && (a.startsWith(b) || b.startsWith(a))) || commonPrefix(a, b) >= 8;

/** 最长公共子序列：同类型且首行文字相同（框类块只看类型）的块才算对应 */
function align(A, B, match) {
  const n = A.length, m = B.length;
  const dp = Array.from({ length: n + 1 }, () => new Int32Array(m + 1));
  for (let i = n - 1; i >= 0; i--) for (let j = m - 1; j >= 0; j--) dp[i][j] = match(A[i], B[j]) ? dp[i + 1][j + 1] + 1 : Math.max(dp[i + 1][j], dp[i][j + 1]);
  const pairs = [], onlyA = [], onlyB = [];
  let i = 0, j = 0;
  while (i < n && j < m) {
    if (match(A[i], B[j]) && dp[i][j] === dp[i + 1][j + 1] + 1) { pairs.push([i, j]); i++; j++; }
    else if (dp[i + 1][j] >= dp[i][j + 1]) onlyA.push(i++);
    else onlyB.push(j++);
  }
  while (i < n) onlyA.push(i++);
  while (j < m) onlyB.push(j++);
  return { pairs, onlyA, onlyB };
}

const num = v => parseFloat(v);
const TEXT_PROPS = ['family', 'size', 'weight', 'style', 'lh', 'ls', 'color'];
const INLINE_PROPS = ['family', 'platform', 'size', 'weight', 'style', 'color', 'bg', 'deco', 'pad', 'radius', 'ls', 'va', 'h'];
function propDiff(a, b, props) {
  const d = {};
  for (const p of props) {
    const x = a?.[p], y = b?.[p];
    if (x === undefined && y === undefined) continue;
    if (typeof x === 'number' || /^-?[\d.]+px$/.test(String(x))) { if (Math.abs(num(x) - num(y)) > 0.05 || Number.isNaN(num(y))) d[p] = [x, y]; }
    else if (x !== y) d[p] = [x, y];
  }
  return d;
}

function compare(old, neu) {
  const { pairs, onlyA, onlyB } = align(old.blocks, neu.blocks, (a, b) => a.type === b.type && (BOX_TYPES(a.type) || sameKey(a.key, b.key)));
  const rows = [];
  let prevDy = 0;
  for (const [i, j] of pairs) {
    const a = old.blocks[i], b = neu.blocks[j];
    const dy = b.y - a.y;
    const dInner = a.inner !== undefined && b.inner !== undefined ? +(b.inner - a.inner).toFixed(2) : null;
    const row = {
      type: a.type, key: a.key, oldY: a.y, dy: +dy.toFixed(2), gap: +(dy - prevDy).toFixed(2), dh: +(b.h - a.h).toFixed(2), dInner,
      // mermaid 两边版本不同（8 与 12），同一张图的尺寸本来就不同：框的高度差按扣掉图本身高度差之后算
      dhAdj: +(b.h - a.h - (/mermaid$/.test(a.type) && dInner !== null ? dInner : 0)).toFixed(2),
      dx: +(b.x - a.x).toFixed(2), dw: +(b.w - a.w).toFixed(2), dTextX: a.textX !== undefined && b.textX !== undefined ? +(b.textX - a.textX).toFixed(2) : 0,
      style: propDiff(a.text, b.text, TEXT_PROPS),
    };
    if (a.lh !== null && Math.abs(num(a.lh) - num(b.lh)) > 0.05) row.style.blockLh = [a.lh, b.lh];
    if ((a.platform ?? '') !== (b.platform ?? '') && a.text && b.text) row.style.platform = [a.platform, b.platform];
    prevDy = dy;
    rows.push(row);
  }
  const inl = [];
  for (const kind of new Set([...old.inlines, ...neu.inlines].map(x => x.kind))) {
    const A = old.inlines.filter(x => x.kind === kind), B = neu.inlines.filter(x => x.kind === kind);
    const r = align(A, B, (a, b) => sameKey(a.key, b.key));
    for (const [i, j] of r.pairs) inl.push({ kind, key: A[i].key, diff: propDiff(A[i], B[j], INLINE_PROPS) });
    for (const i of r.onlyA) inl.push({ kind, key: A[i].key, only: 'old' });
    for (const j of r.onlyB) inl.push({ kind, key: B[j].key, only: 'new' });
  }
  return {
    rows, inl,
    onlyOld: onlyA.map(i => ({ type: old.blocks[i].type, key: old.blocks[i].key })),
    onlyNew: onlyB.map(j => ({ type: neu.blocks[j].type, key: neu.blocks[j].key })),
    endDrift: rows.length ? rows[rows.length - 1].dy : 0,
    bg: [old.bg, neu.bg],
  };
}

// ── 主流程 ──
const results = [];
const t0 = Date.now();
const oldP = await new OldPage(oldStatics, port, basePath).open();
const newP = await new NewPage(newDist, port + 1, basePath).open();
try {
  const shotDir = args.shots ? resolve(args.shots) : null;
  if (shotDir) mkdirSync(shotDir, { recursive: true });
  for (const name of docNames) {
    if (!corpus.has(name)) { console.warn(`没有文档 ${name}`); continue; }
    const text = withSentinel(corpus.get(name));
    for (const cfg of matrix) {
      const tt = [Date.now()];
      await oldP.apply(cfg, text); tt.push(Date.now());
      await newP.apply(cfg, text); tt.push(Date.now());
      const o = await oldP.measure(); tt.push(Date.now());
      const n = await newP.measure(); tt.push(Date.now());
      const c = compare(o, n);
      results.push({ doc: name, cfg: cfgName(cfg), ...c, old: verbose ? o : undefined, new: verbose ? n : undefined });
      console.log(`${name.padEnd(26)} ${cfgName(cfg).padEnd(18)} 块 ${c.rows.length}/${o.blocks.length}/${n.blocks.length}  末尾漂移 ${c.endDrift}  最大|Δh| ${Math.max(0, ...c.rows.map(r => Math.abs(r.dh)))}` + (verbose ? `  （旧装载 ${tt[1] - tt[0]} 新装载 ${tt[2] - tt[1]} 采集 ${tt[4] - tt[2]} ms）` : ''));
      if (shotDir && name === 'showcase' && cfg.width === 1280 && cfg.font === FONT_DEFAULT) {
        await oldP.shot(join(shotDir, `${name}-${cfg.theme}-old.png`), cfg.width);
        await newP.shot(join(shotDir, `${name}-${cfg.theme}-new.png`), cfg.width);
      }
    }
  }
} finally {
  await oldP.close();
  await newP.close();
}

// ── 报告 ──
const KNOWN = JSON.parse(readFileSync(join(here, 'known.json'), 'utf8'));
// 条目：{ docs?: [文档名], match: 正则, reason: 说明 }；match 对着「种类 块类型 首行文字」匹配，种类是
// gap / dh / dx / style.<属性> / only-old / only-new / inline.<属性> / inline-only-old / inline-only-new
const known = (doc, what) => KNOWN.some(k => (!k.docs || k.docs.includes(doc)) && new RegExp(k.match).test(what));
const TOL = { drift: 2, dh: 1, dx: 1 };
const lines = [];
const fails = [];
lines.push(`# 排版数值对比（旧 Muya vs 新 CM6）`, '', `${new Date().toISOString()}，${results.length} 组，用时 ${((Date.now() - t0) / 1000).toFixed(0)} s。旧页面：${oldStatics}；新页面：${newDist}。`, '');
lines.push('| 文档 | 配置 | 对应块 | 仅旧 | 仅新 | 末尾漂移（原始 → 扣除已说明差异） | 最大\\|Δy\\| | 最大\\|Δh\\| | 最大\\|Δx\\|/\\|Δw\\| | 样式差异块 | 行内差异 |', '|---|---|---|---|---|---|---|---|---|---|---|');
for (const r of results) {
  const mx = k => Math.max(0, ...r.rows.map(x => Math.abs(x[k])));
  const styled = r.rows.filter(x => Object.keys(x.style).length).length;
  const inlineBad = r.inl.filter(x => x.only || Object.keys(x.diff).length).length;
  // 已说明的差异（known.json 里的 dh / gap 条目，以及 mermaid 图本身的尺寸差）不计入累计漂移：
  // dyAdj = dy − 此前所有已说明的高度差与间距差之和
  let excused = 0;
  for (const x of r.rows) {
    const what = `${x.type} ${x.key}`;
    if (Math.abs(x.gap) > 0.5 && known(r.doc, `gap ${what}`)) excused += x.gap;
    x.dyAdj = +(x.dy - excused).toFixed(2);
    // 已说明的高度差不论大小都扣除（表格里行内代码撑高行盒这类差异常常不到 1px，但会一块块累积）
    x.dhKnown = Math.abs(x.dhAdj) > 0.05 && known(r.doc, `dh ${what}`);
    excused += x.dh - (x.dhKnown ? 0 : x.dhAdj);
  }
  r.endDriftAdj = r.rows.length ? r.rows[r.rows.length - 1].dyAdj : 0;
  lines.push(`| ${r.doc} | ${r.cfg} | ${r.rows.length} | ${r.onlyOld.length} | ${r.onlyNew.length} | ${r.endDrift} → ${r.endDriftAdj} | ${mx('dyAdj')} | ${mx('dhAdj')} | ${mx('dx')}/${mx('dw')} | ${styled} | ${inlineBad} |`);
  const tag = `${r.doc} ${r.cfg}`;
  if (Math.abs(r.endDriftAdj) > TOL.drift) fails.push(`${tag}：末尾漂移 ${r.endDriftAdj}px（未扣除已说明差异时 ${r.endDrift}px）`);
  for (const x of r.rows) {
    const what = `${x.type} ${x.key}`;
    if (Math.abs(x.dhAdj) > TOL.dh && !x.dhKnown) fails.push(`${tag}：${what} 高度差 ${x.dhAdj}px`);
    if ((Math.abs(x.dx) > TOL.dx || Math.abs(x.dw) > TOL.dx) && !known(r.doc, `dx ${what}`)) fails.push(`${tag}：${what} Δx ${x.dx} Δw ${x.dw}`);
    for (const [p, v] of Object.entries(x.style)) if (!known(r.doc, `style.${p} ${what}`)) fails.push(`${tag}：${what} ${p} ${v[0]} → ${v[1]}`);
  }
  for (const x of r.onlyOld) if (!known(r.doc, `only-old ${x.type} ${x.key}`)) fails.push(`${tag}：只在旧编辑器 ${x.type} ${x.key}`);
  for (const x of r.onlyNew) if (!known(r.doc, `only-new ${x.type} ${x.key}`)) fails.push(`${tag}：只在新编辑器 ${x.type} ${x.key}`);
  for (const x of r.inl) {
    if (x.only) { if (!known(r.doc, `inline-only-${x.only} ${x.kind} ${x.key}`)) fails.push(`${tag}：行内 ${x.kind} ${x.key} 只在${x.only === 'old' ? '旧' : '新'}编辑器`); continue; }
    for (const [p, v] of Object.entries(x.diff)) if (!known(r.doc, `inline.${p} ${x.kind} ${x.key}`)) fails.push(`${tag}：行内 ${x.kind} ${x.key} ${p} ${v[0]} → ${v[1]}`);
  }
}
// 按块类型汇总：间距差（与前一块的相对漂移）、高度差、横向、样式属性
const byType = new Map();
for (const r of results) for (const x of r.rows) {
  const t = byType.get(x.type) ?? { n: 0, gap: 0, dh: 0, dx: 0, props: new Set() };
  t.n++; t.gap = Math.max(t.gap, Math.abs(x.gap)); t.dh = Math.max(t.dh, Math.abs(x.dhAdj)); t.dx = Math.max(t.dx, Math.abs(x.dx), Math.abs(x.dw), Math.abs(x.dTextX));
  for (const p of Object.keys(x.style)) t.props.add(p);
  byType.set(x.type, t);
}
lines.push('', '## 按块类型', '', '| 块类型 | 次数 | 最大\\|Δ间距\\| | 最大\\|Δh\\| | 最大横向差 | 不一致的样式属性 |', '|---|---|---|---|---|---|');
for (const [t, v] of [...byType].sort()) lines.push(`| ${t} | ${v.n} | ${v.gap} | ${v.dh} | ${v.dx} | ${[...v.props].join(', ') || '—'} |`);
const byKind = new Map();
for (const r of results) for (const x of r.inl) {
  const t = byKind.get(x.kind) ?? { n: 0, only: 0, props: new Set() };
  t.n++; if (x.only) t.only++; else for (const p of Object.keys(x.diff)) t.props.add(p);
  byKind.set(x.kind, t);
}
lines.push('', '## 行内元素', '', '| 种类 | 次数 | 未对应 | 不一致的样式属性 |', '|---|---|---|---|');
for (const [k, v] of [...byKind].sort()) lines.push(`| ${k} | ${v.n} | ${v.only} | ${[...v.props].join(', ') || '—'} |`);
const uniq = [...new Set(fails)];
lines.push('', `## 未达标（${uniq.length} 条，已排除 known.json 里说明过的差异）`, '', ...uniq.slice(0, 200).map(f => `- ${f}`));
if (uniq.length > 200) lines.push(`- ……另 ${uniq.length - 200} 条见 detail.json`);
writeFileSync(join(out, 'report.md'), lines.join('\n') + '\n');
writeFileSync(join(out, 'detail.json'), JSON.stringify({ results, fails: uniq }, null, 1));
console.log(`\n报告：${join(out, 'report.md')}（明细 detail.json）；未达标 ${uniq.length} 条，用时 ${((Date.now() - t0) / 1000).toFixed(0)} s`);
process.exit(uniq.length ? 1 : 0);
