import type { Extension } from '@codemirror/state';
import { EditorView } from '@codemirror/view';
import { redo, undo } from '@codemirror/commands';
import { Channel, type Transport } from './bridge/channel';
import { DocSync } from './bridge/docSync';
import { FrameQueue, Slot } from './bridge/frame';
import { StateReporters } from './bridge/reporters';
import { PROTOCOL_VERSION, type DocLoadPayload, type EditorInitState, type EditorTheme, type KeyChord } from './bridge/protocol';
import { createEditor, type EditorOptions, type TypedownEditor } from './editor/createEditor';
import { headingIds, outlineField } from './editor/state/outline';
import { openLinkHandler } from './editor/decorations/inlinePlugin';
import { parsedLength } from './editor/state/parseProgress';
import { blockField } from './editor/widgets/blockField';
import { FaultReporter } from './host/fault';
import { Keymap, installShortcutBridge } from './host/keyboard';
import { SettingsApplier, applyTheme } from './host/theme';
import { ViewportReporter } from './host/viewport';

/**
 * 页面装配：初始态 → CM6 → 信道与各上报器 → lifecycle.ready（docs/editor-protocol.md 第 3 节）。
 * main.ts（宿主）与 dev 页（假宿主）共用这一条启动路径，性能探针测到的就是宿主下的按键路径。
 */

export const ENGINE = `typedown-editor-next/${__ENGINE_VERSION__} codemirror6`;

export interface AppOptions {
  parent: HTMLElement;
  transport: Transport;
  /** window.__typedownInit；缺失（dev 页、普通浏览器）时用默认值 */
  init?: Partial<EditorInitState> | null;
  /** 额外的 CM6 扩展（dev 页的按键测时） */
  extensions?: Extension[];
  editor?: Pick<EditorOptions, 'nestedCode' | 'frontmatter'>;
  win?: Window;
  /** 帧调度器，默认 requestAnimationFrame */
  raf?: (run: () => void) => void;
  statsDelay?: number;
}

export interface EditorApp {
  editor: TypedownEditor;
  view: EditorView;
  channel: Channel;
  frames: FrameQueue;
  docSync: DocSync;
  init: EditorInitState;
}

/** 读初始态并补齐缺省值；keymap 同时容忍和弦数组与 {chords} 两种形态 */
export function normalizeInit(raw: Partial<EditorInitState> | null | undefined, prefersDark = false): EditorInitState {
  const r = (raw ?? {}) as Omit<Partial<EditorInitState>, 'keymap'> & { keymap?: KeyChord[] | { chords?: KeyChord[] } };
  const keymap = Array.isArray(r.keymap) ? r.keymap : Array.isArray(r.keymap?.chords) ? r.keymap.chords : [];
  const theme: EditorTheme = r.theme && typeof r.theme.isDark === 'boolean' ? r.theme : { isDark: prefersDark } as EditorTheme;
  return {
    protocol: typeof r.protocol === 'number' ? r.protocol : PROTOCOL_VERSION,
    settings: r.settings ?? {},
    theme,
    keymap,
    locale: typeof r.locale === 'string' ? r.locale : 'en-US',
  };
}

/** 首屏就绪：语法树与块组件都覆盖到了首个视口（行内显形靠语法树，块组件靠 blockField 的扫描终点） */
export function firstViewportReady(view: EditorView): boolean {
  const s = view.state;
  const end = Math.min(view.viewport.to, s.doc.length);
  const blocks = s.field(blockField, false);
  if (!blocks) return true; // 源码模式没有显形层
  return parsedLength(s) >= end && blocks.covered >= end;
}

export function startEditorApp(opts: AppOptions): EditorApp {
  const win = opts.win ?? window;
  const init = normalizeInit(opts.init, win.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false);
  if (init.protocol !== PROTOCOL_VERSION) console.warn(`[bridge] 初始态的协议版本 ${init.protocol} 与页面的 ${PROTOCOL_VERSION} 不一致`);

  let started = false;
  const fault: FaultReporter = new FaultReporter(p => channel.emit('lifecycle.fault', p));
  // 启动失败与 doc.load 失败时编辑器状态不可信（fatal），其余只记日志
  const channel = new Channel(opts.transport, { onHandlerError: (t, e) => fault.report(e, !started || t === 'doc.load') });
  fault.install(win, () => !started);

  const frames = new FrameQueue(opts.raf);
  applyTheme(init.theme);

  let editor: TypedownEditor;
  const view = () => editor.view;
  const reporters = new StateReporters({
    emit: (t, p) => channel.emit(t, p),
    frames,
    view,
    sourceMode: () => editor.sourceMode,
    statsDelay: opts.statsDelay,
  });
  const docSync = new DocSync({
    post: m => channel.post(m),
    schedule: f => frames.schedule(Slot.Doc, f),
    scheduleRender: f => frames.schedule(Slot.Other, f),
    createState: p => editor.createState(p.text, clampSelection(p)),
    firstViewportReady,
  });
  const s = init.settings;
  editor = createEditor({
    doc: '',
    parent: opts.parent,
    sourceMode: s.sourceCode ?? false,
    tabSize: typeof s.tabSize === 'number' && s.tabSize > 0 ? s.tabSize : undefined,
    spellcheck: s.spellcheckEnabled ?? false,
    ...opts.editor,
    extensions: [
      docSync.extension,
      EditorView.updateListener.of(u => reporters.onUpdate(u)),
      EditorView.exceptionSink.of(e => { console.error(e); fault.report(e, false); }),
      // Ctrl+单击链接交给宿主打开（view.openLink）
      openLinkHandler.of(uri => channel.emit('view.openLink', { uri })),
      opts.extensions ?? [],
    ],
  });
  const settings = new SettingsApplier({
    setSourceMode: on => editor.setSourceMode(on),
    setTabSize: n => editor.setTabSize(n),
    setSpellcheck: on => editor.setSpellcheck(on),
    remeasure: () => editor.view.requestMeasure(),
  });
  settings.apply(init.settings);

  const keymap = new Keymap();
  keymap.set(init.keymap);
  installShortcutBridge(win.document, keymap, chord => channel.emit('view.shortcut', chord));

  const viewport = new ViewportReporter(win, p => channel.emit('view.viewport', p), task => frames.schedule(Slot.Other, task));
  viewport.install();

  channel
    .onCommand('doc.load', p => {
      docSync.load(view(), p);
      reporters.reset();
      if (typeof p.scrollTop === 'number') {
        // 等 CM6 的首次测量（它的 rAF 先于本帧队列登记）之后再滚，高度估算才对得上
        const y = p.scrollTop;
        frames.schedule(Slot.Other, () => viewport.scrollTo(win.scrollX, y));
      } else {
        if (win.scrollX || win.scrollY) win.scrollTo(0, 0);
        if (p.selection) view().dispatch({ effects: EditorView.scrollIntoView(view().state.selection.main.head, { y: 'center' }) });
      }
      viewport.request(true);
    })
    .onCommand('history.undo', () => { undo(view()); })
    .onCommand('history.redo', () => { redo(view()); })
    .onCommand('history.clear', () => editor.clearHistory())
    .onCommand('outline.reveal', p => {
      const outline = view().state.field(outlineField, false);
      if (!outline) return;
      const i = headingIds(outline.headings).indexOf(p.id);
      if (i >= 0) view().dispatch({ effects: EditorView.scrollIntoView(outline.headings[i].from, { y: 'start' }) });
    })
    .onCommand('view.theme', p => applyTheme(p.theme))
    .onCommand('view.settings', p => settings.apply(p.changes))
    .onCommand('view.keymap', p => keymap.set(p.chords))
    .onCommand('view.scrollTo', p => viewport.scrollTo(p.x, p.y))
    .onCommand('view.refreshViewport', () => viewport.request(true))
    .onRequest('doc.flush', () => docSync.flush())
    .onRequest('doc.getText', () => docSync.getText(view()));

  channel.open();
  started = true;
  channel.emit('lifecycle.ready', { protocol: PROTOCOL_VERSION, engine: ENGINE });
  viewport.request(true);
  return { editor, view: editor.view, channel, frames, docSync, init };
}

/** doc.load 的选区越界时夹到正文范围内 */
function clampSelection(p: DocLoadPayload): { anchor: number; head: number } | undefined {
  const sel = p.selection;
  if (!sel || typeof sel.anchor !== 'number' || typeof sel.head !== 'number') return undefined;
  const clamp = (n: number) => Math.max(0, Math.min(p.text.length, Math.floor(n)));
  return { anchor: clamp(sel.anchor), head: clamp(sel.head) };
}
