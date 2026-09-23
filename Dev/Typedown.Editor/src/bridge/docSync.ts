import type { ChangeSet, EditorState, Extension } from '@codemirror/state';
import { EditorView } from '@codemirror/view';
import type { DocLoadPayload, HostToPage, PageToHost, WireChange } from './protocol';

/**
 * 页面侧正文同步（docs/editor-protocol.md 第 4 节）：每个改动正文的事务把 ChangeSet 合成进待发的累积 ChangeSet，
 * 下一个动画帧用 iterChanges 展开成 [{from, to, insert}] 发一条 doc.changed；doc.flush 先同步发出挂起的增量再应答版本号。
 * 与传输层解耦：post 由调用方提供（宿主接入时是 chrome.webview.postMessage(JSON.stringify(...))）。
 */

export interface DocSyncOptions {
  post: (msg: PageToHost) => void;
  /** 默认 requestAnimationFrame；单测里注入同步或手动触发的调度器 */
  schedule?: (f: () => void) => void;
  /** doc.load 时按新正文建一个全新的 EditorState（清空撤销历史） */
  createState: (payload: DocLoadPayload) => EditorState;
}

export class DocSync {
  version = 0;
  private pending: ChangeSet | null = null;
  private scheduled = false;
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
      this.schedule(() => { this.scheduled = false; this.emit(); });
    }
  });

  /** 把挂起的增量立即发出（帧回调与 doc.flush 共用） */
  emit() {
    const set = this.pending;
    if (!set || set.empty) { this.pending = null; return; }
    this.pending = null;
    this.opts.post({ k: 'evt', t: 'doc.changed', p: { baseVersion: this.version, version: this.version + 1, changes: toWireChanges(set) } });
    this.version++;
  }

  handle(view: EditorView, msg: HostToPage) {
    switch (msg.t) {
      case 'doc.load': {
        this.pending = null;
        this.version = msg.p.version;
        // setState 不经过事务，整篇替换不会回发 doc.changed；新状态需要带着 this.extension
        view.setState(this.opts.createState(msg.p));
        const version = msg.p.version;
        // 首屏画完（两帧后）报 rendered
        this.schedule(() => this.schedule(() => this.opts.post({ k: 'evt', t: 'doc.rendered', p: { version } })));
        break;
      }
      case 'doc.flush':
        this.emit();
        this.opts.post({ k: 'res', id: msg.id, ok: true, p: { version: this.version } });
        break;
      case 'doc.getText':
        this.emit();
        this.opts.post({ k: 'res', id: msg.id, ok: true, p: { version: this.version, text: view.state.doc.toString() } });
        break;
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
