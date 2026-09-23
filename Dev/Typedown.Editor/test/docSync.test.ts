// @vitest-environment jsdom
import { beforeAll, describe, expect, it } from 'vitest';
import { EditorState } from '@codemirror/state';
import { EditorView } from '@codemirror/view';
import { history, undo } from '@codemirror/commands';
import { DocSync, applyWireChanges } from '../src/bridge/docSync';
import type { PageToHost, DocChangedPayload } from '../src/bridge/protocol';
import { markdownSupport } from '../src/editor/syntax';
import { sampleDoc } from '../src/dev/sampleDocs';
import { rng } from './helpers';

beforeAll(() => {
  const rects = () => Object.assign([], { item: () => null }) as unknown as DOMRectList;
  Range.prototype.getClientRects = rects;
  Range.prototype.getBoundingClientRect = () => ({ left: 0, right: 0, top: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0, toJSON() {} }) as DOMRect;
});

/** 宿主镜像：按 docs/editor-protocol.md 第 4 节的规则应用 doc.changed */
class Mirror {
  text = '';
  version = 0;
  stale = 0;
  resyncNeeded = false;
  receive(msg: PageToHost) {
    if (msg.k !== 'evt' || msg.t !== 'doc.changed') return;
    const p = msg.p as DocChangedPayload;
    if (p.baseVersion < this.version) { this.stale++; return; }
    if (p.baseVersion > this.version) { this.resyncNeeded = true; return; }
    for (let i = 1; i < p.changes.length; i++) expect(p.changes[i].from).toBeGreaterThanOrEqual(p.changes[i - 1].to);
    this.text = applyWireChanges(this.text, p.changes);
    this.version = p.version;
  }
}

function setup(doc: string) {
  const frames: (() => void)[] = [];
  const sent: PageToHost[] = [];
  const mirror = new Mirror();
  const sync: DocSync = new DocSync({
    post: m => { sent.push(m); mirror.receive(m); },
    schedule: f => frames.push(f),
    createState: p => EditorState.create({ doc: p.text, extensions: [markdownSupport(), history(), sync.extension], selection: p.selection ?? undefined }),
  });
  const view = new EditorView({ state: EditorState.create({ doc: '', extensions: [markdownSupport(), history(), sync.extension] }), parent: document.body });
  const runFrames = () => { while (frames.length) frames.shift()!(); };
  sync.handle(view, { k: 'cmd', t: 'doc.load', p: { version: 7, text: doc, basePath: '' } });
  mirror.text = doc; mirror.version = 7;
  runFrames();
  return { view, sync, sent, mirror, runFrames, frames };
}

describe('正文同步 doc.changed / doc.flush / doc.getText', () => {
  it('doc.load 用宿主的版本号，首屏后报 doc.rendered，整篇替换不回发 doc.changed', () => {
    const { sent, view } = setup('# a\n');
    expect(sent).toEqual([{ k: 'evt', t: 'doc.rendered', p: { version: 7 } }]);
    expect(view.state.doc.toString()).toBe('# a\n');
  });

  it('随机编辑（含撤销、同一帧多次事务）：每帧后镜像 ≡ 页面正文，版本号逐批加一', () => {
    const random = rng(7);
    const { view, sync, mirror, runFrames, sent } = setup(sampleDoc('rich').slice(0, 8000));
    for (let i = 0; i < 500; i++) {
      const len = view.state.doc.length;
      const r = random();
      if (r < 0.1) undo(view);
      else {
        const from = Math.floor(random() * len);
        const to = Math.min(len, from + (r < 0.4 ? Math.floor(random() * 20) : 0));
        const insert = ['a', '中', '\n', '**', '| x |', '', '😀'][Math.floor(random() * 7)];
        view.dispatch({ changes: { from, to, insert } });
        if (random() < 0.3) view.dispatch({ changes: { from: 0, insert: 'z' } }); // 同一帧里的第二个事务
      }
      if (random() < 0.5) runFrames();
      if (random() < 0.05) {
        sync.handle(view, { k: 'req', id: i, t: 'doc.flush', p: {} });
        const res = sent[sent.length - 1];
        expect(res).toEqual({ k: 'res', id: i, ok: true, p: { version: sync.version } });
        expect(mirror.text).toBe(view.state.doc.toString());
      }
    }
    runFrames();
    expect(mirror.resyncNeeded).toBe(false);
    expect(mirror.text).toBe(view.state.doc.toString());
    expect(mirror.version).toBe(sync.version);
  });

  it('doc.getText 先发出挂起的增量，再应答版本号与全文', () => {
    const { view, sync, sent, mirror } = setup('abc');
    view.dispatch({ changes: { from: 3, insert: 'd' } });
    sync.handle(view, { k: 'req', id: 1, t: 'doc.getText', p: {} });
    expect(sent.at(-2)).toMatchObject({ k: 'evt', t: 'doc.changed', p: { baseVersion: 7, version: 8, changes: [{ from: 3, to: 3, insert: 'd' }] } });
    expect(sent.at(-1)).toEqual({ k: 'res', id: 1, ok: true, p: { version: 8, text: 'abcd' } });
    expect(mirror.text).toBe('abcd');
  });

  it('重新 doc.load 后，旧版本的在途 doc.changed 被镜像丢弃', () => {
    const { view, sync, mirror, frames } = setup('abc');
    view.dispatch({ changes: { from: 0, insert: 'x' } });
    const pendingFrame = frames.shift()!;
    // 宿主在增量发出前装载了新文档
    sync.handle(view, { k: 'cmd', t: 'doc.load', p: { version: 20, text: 'new', basePath: '' } });
    mirror.text = 'new'; mirror.version = 20;
    pendingFrame();
    expect(mirror.text).toBe('new');
    expect(view.state.doc.toString()).toBe('new');
  });
});
