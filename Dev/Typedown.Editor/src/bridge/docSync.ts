import type { ChangeSet, EditorState, Extension } from '@codemirror/state';
import { EditorView } from '@codemirror/view';
import type { DocChanged, DocFlush, DocFlushResult, DocGetText, DocGetTextResult, DocLoad, DocLoadPayload, DocRendered, Response, WireChange } from './protocol';

/**
 * 页面侧正文同步（docs/editor-protocol.md 第 4 节）：每个改动正文的事务把 ChangeSet 合成进待发的累积 ChangeSet，
 * 下一个动画帧用 iterChanges 展开成 [{from, to, insert}] 发一条 doc.changed；doc.flush 先同步发出挂起的增量再应答版本号。
 * 与传输层解耦：post 由调用方提供（宿主接入时经 bridge/channel.ts 发出）。
 */

export type DocOutbound = DocChanged | DocRendered | Response<DocFlushResult> | Response<DocGetTextResult>;
export type DocInbound = DocLoad | DocFlush | DocGetText;

export interface DocSyncOptions {
  post: (msg: DocOutbound) => void;
  /** 默认 requestAnimationFrame；接宿主时排进 FrameQueue 的 Doc 槽位，单测里注入手动触发的调度器 */
  schedule?: (f: () => void) => void;
  /** doc.load 时按新正文建一个全新的 EditorState（清空撤销历史） */
  createState: (payload: DocLoadPayload) => EditorState;
  /**
   * 首屏是否就绪（首个视口的装饰与块组件都已就位）。doc.load 后从第二帧起每帧问一次，为真或问满 maxRenderFrames 次后发 doc.rendered。
   * 缺省时第二帧就发。
   */
  firstViewportReady?: (view: EditorView) => boolean;
  /** 默认 30 帧（约 0.5 s），防止某个条件永远不满足时宿主一直不显示 WebView */
  maxRenderFrames?: number;
  /** doc.rendered 轮询用的调度器；缺省同 schedule。接宿主时排进「其他」槽位，保持同帧事件的顺序 */
  scheduleRender?: (f: () => void) => void;
}

export class DocSync {
  version = 0;
  private pending: ChangeSet | null = null;
  private scheduled = false;
  /** 每次 doc.load 递增，过期的 rendered 轮询据此作废 */
  private loadSeq = 0;
  private readonly schedule: (f: () => void) => void;

  constructor(private readonly opts: DocSyncOptions) {
    this.schedule = opts.schedule ?? (f => requestAnimationFrame(() => f()));
  }

  /** 挂到编辑器上的扩展：收集改动正文的事务 */
  readonly extension: Extension = EditorView.updateListener.of(u => {
    for (const tr of u.transactions) {
      if (!tr.docChanged) continue;
      this.pending = this.pending ? this.pending.compose(tr.changes) : tr.changes;
    }
    if (this.pending && !this.scheduled) {
      this.scheduled = true;
      this.schedule(this.onFrame);
    }
  });

  private readonly onFrame = () => { this.scheduled = false; this.emit(); };

  /** 是否有尚未发出的增量 */
  get hasPending() { return this.pending !== null && !this.pending.empty; }

  /** 把挂起的增量立即发出（帧回调与 doc.flush 共用） */
  emit() {
    const set = this.pending;
    if (!set || set.empty) { this.pending = null; return; }
    this.pending = null;
    this.opts.post({ k: 'evt', t: 'doc.changed', p: { baseVersion: this.version, version: this.version + 1, changes: toWireChanges(set) } });
    this.version++;
  }

  /** doc.load：整篇替换正文并清空撤销历史，首屏就绪后报 doc.rendered */
  load(view: EditorView, payload: DocLoadPayload) {
    this.pending = null;
    this.version = payload.version;
    // setState 不经过事务，整篇替换不会回发 doc.changed；新状态需要带着 this.extension
    view.setState(this.opts.createState(payload));
    const seq = ++this.loadSeq, version = payload.version;
    const ready = this.opts.firstViewportReady;
    const max = this.opts.maxRenderFrames ?? 30;
    const schedule = this.opts.scheduleRender ?? this.schedule;
    let frames = 0;
    const poll = () => {
      if (seq !== this.loadSeq) return; // 又来了一次 doc.load，这一轮作废
      frames++;
      if (frames >= 2 && (!ready || ready(view) || frames >= max)) this.opts.post({ k: 'evt', t: 'doc.rendered', p: { version } });
      else schedule(poll);
    };
    schedule(poll);
  }

  /** doc.flush：先同步发出挂起的增量，再给出当前版本号 */
  flush(): DocFlushResult {
    this.emit();
    return { version: this.version };
  }

  /** doc.getText：先发出挂起的增量，再给出版本号与全文 */
  getText(view: EditorView): DocGetTextResult {
    this.emit();
    return { version: this.version, text: view.state.doc.toString() };
  }

  /** 不经信道、直接按报文处理（单测与不需要信道的场合） */
  handle(view: EditorView, msg: DocInbound) {
    switch (msg.t) {
      case 'doc.load': this.load(view, msg.p); break;
      case 'doc.flush': { const p = this.flush(); this.opts.post({ k: 'res', id: msg.id, ok: true, p }); break; }
      case 'doc.getText': { const p = this.getText(view); this.opts.post({ k: 'res', id: msg.id, ok: true, p }); break; }
    }
  }
}

/** ChangeSet → 引擎无关的替换列表（按 from 升序、互不重叠、偏移相对变更前的正文） */
export function toWireChanges(set: ChangeSet): WireChange[] {
  const out: WireChange[] = [];
  set.iterChanges((fromA, toA, _fromB, _toB, inserted) => out.push({ from: fromA, to: toA, insert: inserted.toString() }));
  return out;
}

/** 宿主镜像的应用方式（逆序应用），单测与将来的契约样例共用 */
export function applyWireChanges(text: string, changes: readonly WireChange[]): string {
  for (let i = changes.length - 1; i >= 0; i--) {
    const c = changes[i];
    text = text.slice(0, c.from) + c.insert + text.slice(c.to);
  }
  return text;
}
