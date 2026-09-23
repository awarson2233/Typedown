// @vitest-environment jsdom
import { beforeAll, describe, expect, it } from 'vitest';
import { startEditorApp, normalizeInit } from '../src/app';
import { FakeHost } from '../src/bridge/channel';
import { applyWireChanges } from '../src/bridge/docSync';
import type { EventType, PageToHost } from '../src/bridge/protocol';

beforeAll(() => {
  const rects = () => Object.assign([], { item: () => null }) as unknown as DOMRectList;
  Range.prototype.getClientRects = rects;
  Range.prototype.getBoundingClientRect = () => ({ left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0, toJSON() {} }) as DOMRect;
  (globalThis as { ResizeObserver?: unknown }).ResizeObserver ??= class { observe() {} unobserve() {} disconnect() {} };
});

/** 宿主镜像：按协议第 4 节应用 doc.changed */
class Mirror {
  text = '';
  version = 0;
  receive(m: PageToHost) {
    if (m.k !== 'evt' || m.t !== 'doc.changed') return;
    expect(m.p.baseVersion).toBe(this.version);
    this.text = applyWireChanges(this.text, m.p.changes);
    this.version = m.p.version;
  }
}

function boot() {
  const host = new FakeHost();
  const mirror = new Mirror();
  host.onMessage(m => mirror.receive(m));
  const frames: (() => void)[] = [];
  const runFrames = () => { for (let i = 0; frames.length && i < 1000; i++) frames.shift()!(); };
  const parent = document.createElement('div');
  document.body.replaceChildren(parent);
  const app = startEditorApp({
    parent,
    transport: host,
    init: {
      protocol: 1,
      settings: { fontSize: 18, lineHeight: 1.6, tabSize: 2, spellcheckEnabled: true, editorAreaWidth: '900px', sourceCode: false, typewriter: false },
      theme: { isDark: true, accent: { r: 1, g: 2, b: 3, a: 1 }, background: { r: 10, g: 20, b: 30, a: 0.5 } },
      keymap: [{ key: 83, modifiers: 1 }],
      locale: 'zh-CN',
    },
    raf: run => frames.push(run),
    statsDelay: 0,
  });
  /** 从第 from 条起、页面发出的事件类型名序列 */
  const types = (from = 0) => host.received.slice(from).filter(m => m.k === 'evt').map(m => (m as { t: EventType }).t);
  return { host, mirror, app, runFrames, types };
}

const sleep = (ms: number) => new Promise(r => setTimeout(r, ms));

describe('假宿主驱动的完整握手', () => {
  it('初始态 → ready → doc.load → rendered → 编辑 → flush', async () => {
    const { host, mirror, app, runFrames, types } = boot();
    const root = document.documentElement;

    // 初始态：同步应用主题与设置，挂载完就报 ready
    expect(host.received[0]).toEqual({ k: 'evt', t: 'lifecycle.ready', p: { protocol: 1, engine: expect.stringContaining('typedown-editor-next/') } });
    expect(root.dataset.theme).toBe('dark');
    expect(root.style.getPropertyValue('--td-accent')).toBe('rgba(1, 2, 3, 1)');
    expect(root.style.getPropertyValue('--td-bg')).toBe('rgba(10, 20, 30, 0.5)');
    expect(root.style.getPropertyValue('--td-font-size')).toBe('18px');
    expect(root.style.getPropertyValue('--td-line-height')).toBe('1.6');
    expect(app.view.state.tabSize).toBe(2);
    expect(app.view.contentDOM.getAttribute('spellcheck')).toBe('true');

    // doc.load：版本号来自宿主，首屏后报 rendered，整篇替换不回发 doc.changed
    const text = '# 标题\n\n正文 **粗体** 结尾\n\n## 第二节\n';
    const loadAt = host.received.length;
    host.command('doc.load', { version: 5, text, basePath: 'C:\\docs', selection: { anchor: 3, head: 3 } });
    mirror.text = text; mirror.version = 5;
    runFrames();
    expect(host.events('doc.rendered')).toEqual([{ version: 5 }]);
    expect(types(loadAt)).not.toContain('doc.changed');
    expect(host.events('outline.changed').at(-1)).toEqual({
      items: [{ id: '标题', level: 1, text: '标题' }, { id: '第二节', level: 2, text: '第二节' }],
      current: { id: '标题', level: 1, text: '标题' },
    });
    expect(host.events('selection.changed').at(-1)).toMatchObject({ hasText: false, text: '', anchor: 3, head: 3, rich: { block: { kinds: ['heading1'] } } });
    await sleep(5); runFrames();
    // 11 个汉字 + 3 个非汉字词（#、****、##）；字符数不含空白
    expect(host.events('stats.changed').at(-1)).toEqual({ characters: 18, words: 14 });

    // 编辑：同一帧的事件顺序固定为 doc.changed → history.changed → selection.changed → selection.marks → 其他
    const pos = text.indexOf('粗体') + 1;
    const prefix = '# 新标题\n\n';
    const editAt = host.received.length;
    app.view.dispatch({ changes: { from: pos, insert: 'X' }, selection: { anchor: pos + 1 }, userEvent: 'input.type' });
    app.view.dispatch({ changes: { from: 0, insert: prefix }, selection: { anchor: pos + 1 + prefix.length }, userEvent: 'input.type' });
    runFrames();
    const seq = types(editAt);
    expect(seq.slice(0, 4)).toEqual(['doc.changed', 'history.changed', 'selection.changed', 'selection.marks']);
    expect(seq.filter(t => t === 'doc.changed').length).toBe(1); // 同一帧的两个事务合成一批
    expect(seq).toContain('outline.changed');
    expect(host.events('history.changed').at(-1)).toEqual({ canUndo: true, canRedo: false });
    expect(host.events('selection.marks').at(-1)).toEqual({ marks: ['strong'] });
    expect(host.events('outline.changed').at(-1)!.items.map(i => i.text)).toEqual(['新标题', '标题', '第二节']);
    expect(mirror.text).toBe(app.view.state.doc.toString());

    // flush：先同步发出挂起的增量，再应答版本号
    app.view.dispatch({ changes: { from: app.view.state.doc.length, insert: '尾' } });
    const flushed = await host.request('doc.flush', {});
    expect(flushed).toEqual({ version: mirror.version });
    expect(mirror.version).toBe(7);
    expect(mirror.text).toBe(app.view.state.doc.toString());
    await expect(host.request('doc.getText', {})).resolves.toEqual({ version: 7, text: app.view.state.doc.toString() });

    // 撤销 / 重做 / 清空历史：history.changed 只在翻转时发
    const histAt = host.received.length;
    host.command('history.undo', {});
    runFrames();
    host.command('history.undo', {});
    runFrames();
    const hist = host.received.slice(histAt).filter(m => m.k === 'evt' && m.t === 'history.changed').map(m => (m as { p: unknown }).p);
    expect(hist).toEqual([{ canUndo: true, canRedo: true }]);
    host.command('history.redo', {});
    host.command('history.clear', {});
    runFrames();
    expect(host.events('history.changed').at(-1)).toEqual({ canUndo: false, canRedo: false });
    expect(mirror.text).toBe(app.view.state.doc.toString());

    // 设置与主题的增量；源码模式下 rich 为 null
    host.command('view.settings', { changes: { sourceCode: true, fontSize: null, tabSize: 8 } });
    runFrames();
    expect(app.editor.sourceMode).toBe(true);
    expect(app.view.state.tabSize).toBe(8);
    expect(root.style.getPropertyValue('--td-font-size')).toBe('18px');
    expect(host.events('selection.changed').at(-1)!.rich).toBeNull();
    host.command('view.theme', { theme: { isDark: false, accent: { r: 0, g: 0, b: 0, a: 1 }, background: { r: 255, g: 255, b: 255, a: 1 } } });
    expect(root.dataset.theme).toBe('light');

    // 快捷键：宿主认领的和弦被拦下并回报整数；view.keymap 整表替换
    const press = (init: KeyboardEventInit) => {
      const e = new KeyboardEvent('keydown', { bubbles: true, cancelable: true, ...init });
      app.view.contentDOM.dispatchEvent(e);
      return e;
    };
    expect(press({ key: 's', keyCode: 83, ctrlKey: true } as KeyboardEventInit).defaultPrevented).toBe(true);
    expect(host.events('view.shortcut')).toEqual([{ key: 83, modifiers: 1 }]);
    host.command('view.keymap', { chords: [{ key: 70, modifiers: 1 }] });
    expect(press({ key: 's', keyCode: 83, ctrlKey: true } as KeyboardEventInit).defaultPrevented).toBe(false);
    expect(host.events('view.shortcut').length).toBe(1);

    // 视口：refreshViewport 后下一帧必发
    const vpBefore = host.events('view.viewport').length;
    runFrames();
    expect(host.events('view.viewport').length).toBe(vpBefore);
    host.command('view.refreshViewport', {});
    runFrames();
    expect(host.events('view.viewport').length).toBe(vpBefore + 1);
    expect(host.events('view.viewport').at(-1)).toEqual({ viewportWidth: expect.any(Number), viewportHeight: expect.any(Number), maximumX: expect.any(Number), maximumY: expect.any(Number), scrollX: expect.any(Number), scrollY: expect.any(Number) });

    // 不认识的请求回 unknownType；不认识的命令只记日志
    await expect(host.request('export.renderHtml', { purpose: 'export', title: 'x' })).rejects.toMatchObject({ code: 'unknownType' });
    const n = host.received.length;
    host.command('format.toggle', { mark: 'strong' });
    expect(host.received.length).toBe(n);

    // 再次 doc.load：新版本号、清空历史，旧版本的在途增量被镜像丢弃
    app.view.dispatch({ changes: { from: 0, insert: 'z' } });
    host.command('doc.load', { version: 20, text: 'new', basePath: '' });
    mirror.text = 'new'; mirror.version = 20;
    runFrames();
    expect(host.events('doc.rendered').at(-1)).toEqual({ version: 20 });
    expect(app.view.state.doc.toString()).toBe('new');
    expect(app.editor.sourceMode).toBe(true); // 装载不重置设置
    expect(host.events('outline.changed').at(-1)).toEqual({ items: [], current: null });
  });

  it('lifecycle.fault：同一 message + stack 只报一次；ready 之后的错误不是 fatal', () => {
    const { host } = boot();
    const err = new Error('坏了');
    window.dispatchEvent(new ErrorEvent('error', { error: err, message: err.message }));
    window.dispatchEvent(new ErrorEvent('error', { error: err, message: err.message }));
    expect(host.events('lifecycle.fault')).toEqual([{ message: '坏了', stack: err.stack, fatal: false }]);
  });

  it('初始态缺失或 keymap 写成 {chords} 时补齐默认值', () => {
    expect(normalizeInit(undefined)).toEqual({ protocol: 1, settings: {}, theme: { isDark: false }, keymap: [], locale: 'en-US' });
    expect(normalizeInit({ keymap: { chords: [{ key: 1, modifiers: 0 }] } } as never).keymap).toEqual([{ key: 1, modifiers: 0 }]);
  });
});
