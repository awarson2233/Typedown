// 旧页面（Muya）与新页面（CM6）的无头装载：宿主设置、主题、语料与正文切换，run.mjs 与临时探针共用。
import { mkdirSync, writeFileSync, readFileSync, readdirSync, existsSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { join, dirname, resolve, extname } from 'node:path';
import { deflateSync, crc32 } from 'node:zlib';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { launch, sleep } from '../perf-probe/cdp.mjs';
import { COLLECT_SOURCE } from './collect.mjs';

const here = dirname(fileURLToPath(import.meta.url));
export const repo = resolve(here, '../..');
export const editorDir = join(repo, 'Dev/Typedown.Editor');

// ── 宿主设置（Dev/Typedown.Core/ViewModels/SettingsViewModel.cs 的默认值）与主题（WinUI 的 EditorThemeFactory） ──
export const DEFAULT_SETTINGS = {
  sourceCode: false, typewriter: false, focusMode: false, searchIsCaseSensitive: false, searchIsRegexp: false, searchIsWholeWord: false,
  fontSize: 16, lineHeight: 1.6, autoPairBracket: true, autoPairQuote: true, trimUnnecessaryCodeBlockEmptyLines: false,
  preferLooseListItem: true, autoPairMarkdownSyntax: true, editorAreaWidth: '1200px', tabSize: 4, spellcheckEnabled: false,
};
export const FONT_DEFAULT = { fontSize: DEFAULT_SETTINGS.fontSize, lineHeight: DEFAULT_SETTINGS.lineHeight };
const ACCENT = { r: 0, g: 120, b: 212, a: 1 };
const BACKGROUND = { light: { r: 249, g: 249, b: 249, a: 1 }, dark: { r: 40, g: 40, b: 40, a: 1 } };
const css = c => `rgba(${c.r}, ${c.g}, ${c.b}, ${c.a})`;
const HOST_FLAGS = ['--disable-web-security', '--allow-file-access-from-files'];
/**
 * 无头页面偶尔会被当成不可见（visibilityState 变成 hidden），requestAnimationFrame 随之停摆，
 * CM6 的测量与视口更新就不再发生。关掉后台节流，并在每次装载前把页面拉到前台、模拟焦点。
 * 本机 Edge 会往新建的临时配置里装外部扩展（其中有 Dark Reader，会异步改写页面配色），一律禁用。
 */
const FG_FLAGS = ['--disable-extensions', '--disable-renderer-backgrounding', '--disable-background-timer-throttling', '--disable-backgrounding-occluded-windows'];
async function foreground(b) {
  await b.send('Page.bringToFront');
  await b.send('Emulation.setFocusEmulationEnabled', { enabled: true });
}

// ── 语料 ──
export function mainWorktree() {
  const r = spawnSync('git', ['rev-parse', '--path-format=absolute', '--git-common-dir'], { cwd: repo, encoding: 'utf8' });
  return r.status === 0 ? dirname(r.stdout.trim()) : repo;
}

/** 旧编辑器的参考工作区：检出 work/p0-host-prep（最后一个带 Muya 的提交）的工作树；没有时退回主工作树 */
export function muyaWorktree() {
  const r = spawnSync('git', ['worktree', 'list', '--porcelain'], { cwd: repo, encoding: 'utf8' });
  if (r.status !== 0) return mainWorktree();
  const entry = r.stdout.split(/\r?\n\r?\n/).find(e => /^branch refs\/heads\/work\/p0-host-prep$/m.test(e));
  const path = entry?.match(/^worktree (.+)$/m)?.[1];
  return path ? resolve(path) : mainWorktree();
}

export async function loadCorpus() {
  const { SHOWCASE_DOC, BLOCKS_DOC, IME_DOC } = await import(pathToFileURL(join(editorDir, 'src/dev/sampleDocs.ts')).href);
  const corpus = new Map([['showcase', SHOWCASE_DOC], ['blocks', BLOCKS_DOC], ['ime', IME_DOC]]);
  for (const f of readdirSync(join(repo, 'docs')).filter(f => f.endsWith('.md')).sort()) corpus.set(`docs/${f}`, readFileSync(join(repo, 'docs', f), 'utf8'));
  const legacyReadme = join(muyaWorktree(), 'Dev/Typedown.Editor/README.md');
  if (existsSync(legacyReadme)) corpus.set('legacy-readme', readFileSync(legacyReadme, 'utf8'));
  return corpus;
}

/** 每篇末尾加一个哨兵段落，两边的光标都放在那里（不让任何被测块处于显形态），它的位置就是「全文末尾」 */
export const SENTINEL = '哨兵段落 sentinel。';
export const withSentinel = text => text.replace(/\r\n/g, '\n').replace(/\n*$/, '\n') + '\n' + SENTINEL + '\n';

/** 相对路径图片的样例目录：一张 240×80 的纯色 PNG（blocks 样例的 images/logo.png），两边的 basePath 都指向它 */
export function writeFixture(dir) {
  const w = 240, h = 80, rgb = [0, 120, 212];
  const raw = Buffer.alloc((w * 3 + 1) * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) rgb.forEach((c, i) => { raw[y * (w * 3 + 1) + 1 + x * 3 + i] = c; });
  const chunk = (type, data) => { const len = Buffer.alloc(4); len.writeUInt32BE(data.length); const td = Buffer.concat([Buffer.from(type), data]); const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td) >>> 0); return Buffer.concat([len, td, crc]); };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 2;
  mkdirSync(join(dir, 'images'), { recursive: true });
  writeFileSync(join(dir, 'images/logo.png'), Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]));
  // 正斜杠：旧页面用 path-browserify（posix 语义）拼相对路径，`D:/a/b` 拼出的 `/D:/a/b/images/x.png` 相对 file:// 页面正好落到磁盘上；
  // 新页面两种斜杠都认
  return resolve(dir).replace(/\\/g, '/');
}

/** 旧编辑器产物目录：CRA 构建（static/js/main.*.js）；找不到时返回 null */
export function findOldStatics(dir) {
  const d = resolve(dir ?? join(muyaWorktree(), 'Dev/Typedown.WinUI/Resources/Statics'));
  const js = join(d, 'static/js');
  return existsSync(js) && readdirSync(js).some(f => /^main\..*\.js$/.test(f)) ? d : null;
}

export function buildNew() {
  const r = spawnSync('yarn', ['build:bench'], { cwd: editorDir, encoding: 'utf8', shell: true });
  if (r.status !== 0) throw new Error(`yarn build:bench 失败\n${r.stdout}\n${r.stderr}`);
}

const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.map': 'application/json',
  '.svg': 'image/svg+xml', '.png': 'image/png', '.jpg': 'image/jpeg', '.gif': 'image/gif', '.woff2': 'font/woff2', '.woff': 'font/woff', '.ttf': 'font/ttf' };

export async function setViewport(b, width, height) {
  await b.send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: false });
}

export async function waitFor(b, expr, ms, what) {
  const t0 = Date.now();
  for (;;) {
    const v = await b.evalJs(expr);
    if (v && !v.__error) return v;
    if (Date.now() - t0 > ms) throw new Error(`${what}（${expr}）`);
    await sleep(100);
  }
}

/**
 * 一个编辑器页面：open() 起浏览器并导航，apply(cfg, text) 切主题、字号行高、视口宽度与正文（只发变化的部分），
 * measure() 采集块与行内元素并补上实际渲染字体。
 */
class Page {
  constructor(engine, port, basePath) { this.engine = engine; this.port = port; this.basePath = basePath; this.prev = null; }
  async close() { await this.b?.close(); }
  eval(expr, ms) { return this.b.evalJs(expr, ms); }

  /** 版面稳定：高度、图表与公式个数、图片加载状态连续 4 次采样不变（异步渲染的 mermaid、KaTeX、图片都落定） */
  async settle() {
    const sig = this.engine === 'old'
      ? `JSON.stringify([document.documentElement.scrollHeight, document.querySelectorAll('svg').length, document.querySelectorAll('.katex').length, [...document.images].filter(i => i.complete).length, document.querySelectorAll('.ag-image-loading').length])`
      : `(() => { const v = typedown.view; const ls = v.state.values.find(x => x && x.context && x.tree && typeof x.tree.resolveInner === 'function');
          return JSON.stringify([ls.tree.length >= v.state.doc.length, v.contentDOM.getBoundingClientRect().height, document.querySelectorAll('svg').length, document.querySelectorAll('.katex').length, [...document.images].filter(i => i.complete).length, document.querySelectorAll('.cm-td-image-loading').length]); })()`;
    let last = null, same = 0;
    const t0 = Date.now();
    await sleep(300);
    while (Date.now() - t0 < 20000) {
      const s = await this.b.evalJs(sig);
      if (s === last && (this.engine === 'old' || JSON.parse(s)[0])) { if (++same >= 3) return; } else same = 0;
      last = s;
      await sleep(200);
    }
    console.warn(`  ${this.engine}：20 s 内版面未稳定`);
  }

  async measure() {
    const b = this.b;
    let r;
    for (let attempt = 0; ; attempt++) {
      r = await b.evalJs(`(${COLLECT_SOURCE})(${JSON.stringify(this.engine)})`, 120000);
      if (!r || r.__error) throw new Error(`${this.engine} 采集失败：${r?.__error}`);
      if (!r.missing) break;
      // CM6 还有块没渲染出来（视口没覆盖全文）：再拉一次视口
      if (attempt >= 3) { console.warn(`  ${this.engine}：${r.missing} 个块始终没有渲染（${r.missingAt.slice(0, 5).join('、')}）`); break; }
      await sleep(500);
      await this.expandViewport(this.width);
    }
    await b.send('DOM.enable');
    await b.send('CSS.enable');
    await b.send('DOM.getDocument', { depth: 0 });
    const n = Math.max(-1, ...r.blocks.map(x => x.fontEl ?? -1), ...r.inlines.map(x => x.fontEl)) + 1;
    const fonts = [];
    for (let i = 0; i < n; i++) {
      const o = await b.send('Runtime.evaluate', { expression: `__sp.els[${i}]` });
      const objectId = o.result?.result?.objectId;
      if (!objectId) { fonts.push(''); continue; }
      const node = await b.send('DOM.requestNode', { objectId });
      const f = node.result ? await b.send('CSS.getPlatformFontsForNode', { nodeId: node.result.nodeId }) : null;
      // 按字形数排序的实际字体名（CJK 回退字体也在里面）
      fonts.push((f?.result?.fonts ?? []).sort((a, c) => c.glyphCount - a.glyphCount).map(x => x.familyName).join(' + '));
      await b.send('Runtime.releaseObject', { objectId });
    }
    for (const x of [...r.blocks, ...r.inlines]) if (x.fontEl !== undefined) { x.platform = fonts[x.fontEl]; delete x.fontEl; }
    return r;
  }

  async shot(path, width) {
    const h = Math.min(4000, await this.b.evalJs('document.documentElement.scrollHeight'));
    // 超出视口的部分要 captureBeyondViewport 才会画出来（旧页面不拉高视口）
    await this.b.evalJs('scrollTo(0, 0)');
    const s = await this.b.send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: true, clip: { x: 0, y: 0, width, height: h, scale: 1 } });
    writeFileSync(path, Buffer.from(s.result.data, 'base64'));
  }
}

/** 旧页面：file:// + 宿主旧参数；chrome.webview 桩应答 GetSettings 等 invoke，__wvSend(name, args) 模拟宿主推送 */
export class OldPage extends Page {
  constructor(statics, port, basePath) { super('old', port, basePath); this.statics = statics; }
  async open() {
    const b = this.b = await launch(this.port, [...HOST_FLAGS, ...FG_FLAGS]);
    await b.send('Page.enable');
    await setViewport(b, 1280, 900);
    const answers = {
      GetSettings: { ...DEFAULT_SETTINGS, markdown: '', basePath: this.basePath },
      GetCurrentTheme: { theme: 'Light', accentColor: ACCENT, background: BACKGROUND.light },
      GetStringResources: {}, ContentLoaded: null,
    };
    await b.send('Page.addScriptToEvaluateOnNewDocument', { source: `(() => {
      const listeners = [];
      window.__wvSend = (name, args) => setTimeout(() => listeners.forEach(l => l({ data: JSON.stringify({ name, args }) })), 0);
      const answers = ${JSON.stringify(answers)};
      window.chrome = window.chrome || {};
      window.chrome.webview = {
        postMessage: s => { const m = JSON.parse(s); if (m.type === 'invoke') window.__wvSend(m.id, { code: 0, data: m.name in answers ? answers[m.name] : null }); },
        addEventListener: (t, l) => { if (t === 'message') listeners.push(l); },
      };
      // 宿主的文档创建脚本在首帧前把 html 与 body 刷成主题背景
      document.addEventListener('DOMContentLoaded', () => { const c = ${JSON.stringify(css(BACKGROUND.light))}; document.documentElement.style.backgroundColor = c; document.body.style.backgroundColor = c; });
    })();` });
    await b.send('Page.navigate', { url: pathToFileURL(join(this.statics, 'index.html')).href });
    await waitFor(b, `!!document.querySelector('#ag-editor-id')`, 20000, '旧页面未启动');
    return this;
  }
  async apply(cfg, text) {
    const b = this.b, prev = this.prev;
    await foreground(b);
    if (prev?.theme !== cfg.theme) {
      const t = cfg.theme === 'dark' ? 'Dark' : 'Light';
      await b.evalJs(`__wvSend('ThemeChanged', ${JSON.stringify({ theme: t, accentColor: ACCENT, background: BACKGROUND[cfg.theme] })})`);
      await waitFor(b, `[...document.querySelectorAll('link[id^="link_style_"]')].every(l => l.sheet && l.href.endsWith('/${cfg.theme}.theme.css'))`, 10000, '旧页面主题样式未加载');
    }
    if (prev?.font !== cfg.font) await b.evalJs(`__wvSend('SettingsChanged', ${JSON.stringify(cfg.font)})`);
    await setViewport(b, cfg.width, 900);
    const hasSentinel = `[...document.querySelectorAll('#ag-editor-id > p')].some(p => p.textContent.includes(${JSON.stringify(SENTINEL)}))`;
    if (prev && (prev.text !== text || prev.theme !== cfg.theme)) {
      // 先清空再装载：mermaid 按主题渲染，主题变了要整篇重渲染；同一份正文 Muya 不会重新装载
      await b.evalJs(`__wvSend('SetMarkdown', ${JSON.stringify({ text: '', basePath: this.basePath })})`);
      await waitFor(b, `!(${hasSentinel})`, 20000, '旧页面未清空');
    }
    if (prev?.text !== text || prev?.theme !== cfg.theme) {
      const line = text.split('\n').length - 2;
      await b.evalJs(`__wvSend('SetMarkdown', ${JSON.stringify({ text, cursor: { anchor: { line, ch: 0 }, focus: { line, ch: 0 } }, basePath: this.basePath })})`);
      await waitFor(b, hasSentinel, 20000, '旧页面未装载正文');
    }
    this.prev = { ...cfg, text };
    // Muya 整篇渲染，不需要拉高视口（它的 #editor 有 min-height: 100vh，拉高只会让页面跟着变高）
    await b.evalJs('scrollTo(0, 0)');
    await this.settle();
  }
}

/** 新页面：https://typedown.editor/ 经 CDP 的 Fetch 拦截映射到 bench 产物（等同宿主的虚拟主机映射），本地图片主机映射到磁盘 */
export class NewPage extends Page {
  constructor(dist, port, basePath) { super('new', port, basePath); this.dist = dist; this.version = 1 << 20; }
  async open() {
    const b = this.b = await launch(this.port, FG_FLAGS);
    await b.send('Page.enable');
    await setViewport(b, 1280, 900);
    await b.send('Fetch.enable', { patterns: [{ urlPattern: 'https://typedown.editor/*' }, { urlPattern: 'https://typedown.image/*' }] });
    await b.send('Runtime.enable');
    b.on(async m => {
      // 页面里的未捕获异常（CM6 的更新中途抛错会让 DOM 停在旧状态）照原样报出来
      if (m.method === 'Runtime.exceptionThrown') {
        const d = m.params.exceptionDetails;
        console.warn(`  new：页面异常 ${d.exception?.description?.split('\n').slice(0, 3).join(' | ') ?? d.text}`);
        return;
      }
      if (m.method !== 'Fetch.requestPaused') return;
      const { requestId, request } = m.params;
      const u = new URL(request.url);
      const segs = u.pathname.split('/').filter(Boolean).map(decodeURIComponent);
      const file = u.host === 'typedown.image' ? `${segs[0]}:\\${segs.slice(1).join('\\')}` : join(this.dist, segs.join('/') || 'index.html');
      let body = null;
      try { body = readFileSync(file); } catch { }
      if (!body) { await b.send('Fetch.fulfillRequest', { requestId, responseCode: 404, responseHeaders: [], body: '' }); return; }
      await b.send('Fetch.fulfillRequest', { requestId, responseCode: 200, responseHeaders: [{ name: 'Content-Type', value: MIME[extname(file).toLowerCase()] ?? 'application/octet-stream' }], body: body.toString('base64') });
    });
    const init = { protocol: 1, settings: DEFAULT_SETTINGS, theme: { isDark: false, accent: ACCENT, background: BACKGROUND.light }, keymap: [], locale: 'zh-CN' };
    await b.send('Page.addScriptToEvaluateOnNewDocument', { source: `
      window.__typedownInit = ${JSON.stringify(init)};
      (() => {
        const listeners = [];
        window.__wv = { out: [], send: obj => { const data = JSON.stringify(obj); setTimeout(() => listeners.forEach(l => l({ data })), 0); } };
        window.chrome = window.chrome || {};
        window.chrome.webview = { postMessage: s => { window.__wv.out.push(JSON.parse(s)); }, addEventListener: (t, l) => { if (t === 'message') listeners.push(l); } };
      })();` });
    await b.send('Page.navigate', { url: 'https://typedown.editor/index.html' });
    await waitFor(b, `__wv.out.some(m => m.t === 'lifecycle.ready')`, 20000, '新页面未就绪');
    return this;
  }
  async apply(cfg, text) {
    const b = this.b, prev = this.prev;
    await foreground(b);
    if (prev?.theme !== cfg.theme) await b.evalJs(`__wv.send({ k: 'cmd', t: 'view.theme', p: { theme: ${JSON.stringify({ isDark: cfg.theme === 'dark', accent: ACCENT, background: BACKGROUND[cfg.theme] })} } })`);
    if (prev?.font !== cfg.font) await b.evalJs(`__wv.send({ k: 'cmd', t: 'view.settings', p: { changes: ${JSON.stringify(cfg.font)} } })`);
    await setViewport(b, cfg.width, 900);
    // 主题变化时同样重载正文：mermaid 按主题渲染
    if (prev?.text !== text || prev?.theme !== cfg.theme) {
      const at = text.lastIndexOf(SENTINEL);
      this.version += 1 << 20;
      await b.evalJs(`__wv.send({ k: 'cmd', t: 'doc.load', p: ${JSON.stringify({ version: this.version, text, basePath: this.basePath, selection: { anchor: at, head: at } })} })`);
      await waitFor(b, `typedown.view.state.doc.length === ${text.length} && typedown.view.state.selection.main.head === ${at}`, 20000, '新页面未装载正文');
    }
    this.prev = { ...cfg, text };
    this.width = cfg.width;
    await sleep(100);
    await b.evalJs('scrollTo(0, 0)');
    await this.settle();
    await this.expandViewport(cfg.width);
  }
  /** 把视口拉高到装得下整篇：CM6 只渲染视口附近的行，量全文要让所有行都在视口里 */
  async expandViewport(width) {
    const b = this.b;
    for (let i = 0; i < 6; i++) {
      const need = await b.evalJs(`(() => { const c = typedown.view.contentDOM; return Math.ceil(c.getBoundingClientRect().bottom + scrollY - parseFloat(getComputedStyle(c).paddingBottom)); })()`);
      const cur = await b.evalJs('innerHeight');
      if (need + 50 <= cur) break;
      // 留足余量：未渲染的行高是估算的，渲染出来往往更高
      await setViewport(b, width, Math.min(200000, Math.ceil(need * 1.25) + 1000));
      await b.evalJs('scrollTo(0, 0)');
      await sleep(200);
      await this.settle();
    }
    await this.ensureTop();
  }
  /**
   * 回到顶部并等 CM6 按新的滚动位置重排视口：装载正文时选区在文末的哨兵上，页面会把它滚到视口中间，
   * 这次滚动与视口变化引起的重新测量是异步的，不等它落定就采集会只量到文档尾部那几块
   */
  async ensureTop() {
    const b = this.b;
    await foreground(b);
    await b.evalJs('scrollTo(0, 0); typedown.view.requestMeasure()');
    const ok = `(() => { const v = typedown.view, f = v.contentDOM.firstElementChild;
      return scrollY === 0 && v.viewport.from === 0 && v.viewport.to === v.state.doc.length && !!f && !f.classList.contains('cm-gap') && v.posAtDOM(f, 0) === 0; })()`;
    try { await waitFor(b, ok, 5000, ''); } catch {
      // 交给 measure 的缺块重试
      if (process.env.SP_DEBUG) console.warn('  ensureTop 超时', await b.evalJs(`JSON.stringify({ sy: scrollY, ih: innerHeight, dh: document.documentElement.scrollHeight, vp: typedown.view.viewport, len: typedown.view.state.doc.length, first: typedown.view.contentDOM.firstElementChild?.className, hasFocus: document.hasFocus(), vis: document.visibilityState })`));
    }
  }
}
