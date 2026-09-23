import { afterEach, describe, expect, it, vi } from 'vitest';
import { Channel, ChannelError, FakeHost, decodeEnvelope, invalidField } from '../src/bridge/channel';
import type { PageToHost } from '../src/bridge/protocol';

function setup(open = true) {
  const host = new FakeHost();
  const logs: string[] = [];
  const errors: [string, unknown][] = [];
  const channel = new Channel(host, { log: m => logs.push(m), onHandlerError: (t, e) => errors.push([t, e]) });
  if (open) channel.open();
  const last = () => host.received[host.received.length - 1];
  return { host, channel, logs, errors, last };
}
const tick = () => new Promise(r => setTimeout(r, 0));

afterEach(() => { vi.useRealTimers(); });

describe('信封编解码', () => {
  it('四种报文的合法形态', () => {
    expect(decodeEnvelope('{"k":"cmd","t":"view.refreshViewport","p":{}}')).toEqual({ k: 'cmd', t: 'view.refreshViewport', p: {} });
    expect(decodeEnvelope('{"t":"doc.flush","id":17,"k":"req","p":{}}')).toEqual({ k: 'req', id: 17, t: 'doc.flush', p: {} });
    expect(decodeEnvelope('{"k":"res","id":3,"ok":true,"p":{"rows":2,"columns":3}}')).toEqual({ k: 'res', id: 3, ok: true, p: { rows: 2, columns: 3 } });
    // 结果可空的请求应答可以是 "p":null；缺 p 按 {} 读
    expect(decodeEnvelope('{"k":"res","id":3,"ok":true,"p":null}')).toEqual({ k: 'res', id: 3, ok: true, p: null });
    expect(decodeEnvelope('{"k":"res","id":3,"ok":true}')).toEqual({ k: 'res', id: 3, ok: true, p: {} });
    expect(decodeEnvelope('{"k":"res","id":4,"ok":false,"err":{"code":"canceled","message":"x"}}')).toEqual({ k: 'res', id: 4, ok: false, err: { code: 'canceled', message: 'x' } });
  });

  it('宿主误用 PostWebMessageAsJson 时收到对象也照常解析；缺省载荷按 {}', () => {
    expect(decodeEnvelope({ k: 'cmd', t: 'history.undo' })).toEqual({ k: 'cmd', t: 'history.undo', p: {} });
  });

  it('畸形报文给出原因；请求能取到 id 的带上 reqId', () => {
    expect(decodeEnvelope('{oops')).toMatchObject({ error: expect.any(String) });
    expect(decodeEnvelope('[1]')).toMatchObject({ error: expect.any(String) });
    expect(decodeEnvelope('{"k":"evt","t":"x","p":{}}')).toMatchObject({ error: expect.any(String) });
    expect(decodeEnvelope('{"k":"cmd","p":{}}')).toMatchObject({ error: expect.any(String) });
    expect(decodeEnvelope('{"k":"req","id":-1,"t":"doc.flush","p":{}}')).not.toHaveProperty('reqId');
    expect(decodeEnvelope('{"k":"req","id":1.5,"t":"doc.flush","p":{}}')).not.toHaveProperty('reqId');
    expect(decodeEnvelope('{"k":"req","id":9,"t":"doc.flush","p":[1]}')).toMatchObject({ reqId: 9 });
    expect(decodeEnvelope('{"k":"req","id":9,"p":{}}')).toMatchObject({ reqId: 9 });
    expect(decodeEnvelope('{"k":"res","id":2}')).toMatchObject({ error: expect.any(String) });
  });

  it('不认识的错误码按 failed 处理', () => {
    expect(decodeEnvelope('{"k":"res","id":1,"ok":false,"err":{"code":"weird"}}')).toEqual({ k: 'res', id: 1, ok: false, err: { code: 'failed', message: undefined } });
  });

  it('必填字段校验只针对页面处理的消息', () => {
    expect(invalidField('doc.load', { version: 1, text: '', basePath: '' })).toBeNull();
    expect(invalidField('doc.load', { version: 1, basePath: '' })).toBe('text');
    expect(invalidField('doc.load', { version: '1', text: '', basePath: '' })).toBe('version');
    expect(invalidField('doc.load', { version: 1, text: '', basePath: null })).toBe('basePath');
    expect(invalidField('view.keymap', { chords: {} })).toBe('chords');
    expect(invalidField('view.theme', { theme: null })).toBe('theme');
    expect(invalidField('format.toggle', {})).toBeNull();
  });
});

describe('命令与宿主→页面的请求', () => {
  it('命令即发即走；不认识的命令只记日志、不应答', () => {
    const { host, channel, logs } = setup();
    const got: unknown[] = [];
    channel.onCommand('view.scrollTo', p => got.push(p));
    host.command('view.scrollTo', { x: 0, y: 10 });
    host.command('format.toggle', { mark: 'strong' });
    expect(got).toEqual([{ x: 0, y: 10 }]);
    expect(host.received).toEqual([]);
    expect(logs.some(l => l.includes('format.toggle'))).toBe(true);
  });

  it('载荷不合格的命令不执行', () => {
    const { host, channel, logs } = setup();
    const got: unknown[] = [];
    channel.onCommand('doc.load', p => got.push(p));
    host.command('doc.load', { version: 1, basePath: '' } as never);
    expect(got).toEqual([]);
    expect(logs.length).toBe(1);
  });

  it('请求：成功应答、不认识的类型回 unknownType、载荷不合格回 invalidPayload、处理抛异常回 failed', async () => {
    const { host, channel, errors } = setup();
    channel.onRequest('doc.flush', () => ({ version: 42 }));
    channel.onRequest('doc.getText', () => { throw new Error('boom'); });
    channel.onRequest('selection.contextAt', async () => { await tick(); return null; });
    await expect(host.request('doc.flush', {})).resolves.toEqual({ version: 42 });
    await expect(host.request('selection.contextAt', { x: 1, y: 2 })).resolves.toBeNull();
    await expect(host.request('export.renderHtml', { purpose: 'export', title: 't' })).rejects.toMatchObject({ code: 'unknownType' });
    await expect(host.request('doc.getText', {})).rejects.toMatchObject({ code: 'failed', message: 'boom' });
    expect(errors.map(e => e[0])).toEqual(['doc.getText']);
    host.post('{"k":"req","id":99,"t":"doc.flush","p":"x"}');
    expect(host.received.at(-1)).toMatchObject({ k: 'res', id: 99, ok: false, err: { code: 'invalidPayload' } });
  });

  it('处理函数抛出 ChannelError 时按它的错误码应答', async () => {
    const { host, channel } = setup();
    channel.onRequest('doc.flush', () => { throw new ChannelError('canceled', '重载中'); });
    await expect(host.request('doc.flush', {})).rejects.toMatchObject({ code: 'canceled', message: '重载中' });
  });

  it('open 之前：请求回 notReady，命令排队到 open 后按序执行', async () => {
    const { host, channel } = setup(false);
    const order: string[] = [];
    channel.onCommand('history.undo', () => order.push('undo'));
    channel.onCommand('history.redo', () => order.push('redo'));
    channel.onRequest('doc.flush', () => ({ version: 1 }));
    host.command('history.undo', {});
    host.command('history.redo', {});
    await expect(host.request('doc.flush', {})).rejects.toMatchObject({ code: 'notReady' });
    expect(order).toEqual([]);
    channel.open();
    expect(order).toEqual(['undo', 'redo']);
    await expect(host.request('doc.flush', {})).resolves.toEqual({ version: 1 });
  });

  it('命令处理抛异常：记日志并交给 onHandlerError，通道不中断', () => {
    const { host, channel, errors } = setup();
    let n = 0;
    channel.onCommand('history.undo', () => { n++; throw new Error('x'); });
    host.command('history.undo', {});
    host.command('history.undo', {});
    expect(n).toBe(2);
    expect(errors.length).toBe(2);
  });
});

describe('页面→宿主的请求与挂起表', () => {
  it('id 从 1 递增、与宿主方向的 id 互不相干；应答按 id 了结', async () => {
    const { host, channel } = setup();
    host.respond('table.pickSize', () => ({ rows: 3, columns: 4 }));
    host.respond('image.resolve', p => (p.source.kind === 'webUrl' ? { src: p.source.value } : null));
    channel.onRequest('doc.flush', () => ({ version: 7 }));
    const a = channel.request('table.pickSize', {});
    const b = channel.request('image.resolve', { source: { kind: 'dataUrl', value: 'data:' } });
    await expect(host.request('doc.flush', {})).resolves.toEqual({ version: 7 }); // 宿主的 id 也是 1
    await expect(a).resolves.toEqual({ rows: 3, columns: 4 });
    await expect(b).resolves.toBeNull();
    const reqs = host.received.filter((m): m is Extract<PageToHost, { k: 'req' }> => m.k === 'req');
    expect(reqs.map(r => r.id)).toEqual([1, 2]);
    expect(channel.pendingCount).toBe(0);
  });

  it('宿主回错误码：以 ChannelError 拒绝', async () => {
    const { channel } = setup();
    await expect(channel.request('clipboard.write', { plainText: 'x' })).rejects.toBeInstanceOf(ChannelError);
    await expect(channel.request('clipboard.write', { plainText: 'x' })).rejects.toMatchObject({ code: 'unknownType' });
  });

  it('超时：从挂起表移除，迟到的应答只记日志', async () => {
    vi.useFakeTimers();
    const logs: string[] = [];
    // 不自动应答的传输：deliver 手动投递宿主的应答
    const silent = { sent: [] as string[], send(d: string) { this.sent.push(d); }, listen(f: (d: unknown) => void) { this.deliver = f; }, deliver: (_: unknown) => {} };
    const ch = new Channel(silent, { log: m => logs.push(m) });
    ch.open();
    const q = ch.request('table.pickSize', {}, 1000);
    const noTimeout = ch.request('table.pickSize', {});
    expect(ch.pendingCount).toBe(2);
    vi.advanceTimersByTime(999);
    expect(ch.pendingCount).toBe(2);
    vi.advanceTimersByTime(1);
    await expect(q).rejects.toMatchObject({ code: 'timeout' });
    expect(ch.pendingCount).toBe(1);
    silent.deliver('{"k":"res","id":1,"ok":true,"p":{"rows":1,"columns":1}}');
    expect(logs.some(l => l.includes('没有挂起的请求 1'))).toBe(true);
    silent.deliver('{"k":"res","id":2,"ok":true,"p":null}');
    await expect(noTimeout).resolves.toBeNull();
    expect(JSON.parse(silent.sent[0])).toEqual({ k: 'req', id: 1, t: 'table.pickSize', p: {} });
  });

  it('cancelAll：挂起的请求一律以 canceled 结束', async () => {
    const silent = { send() {}, listen() {} };
    const ch = new Channel(silent, { log: () => {} });
    const a = ch.request('image.resolve', { source: { kind: 'filePath', value: 'C:\\a.png' } });
    const b = ch.request('table.pickSize', {}, 5000);
    ch.cancelAll();
    await expect(a).rejects.toMatchObject({ code: 'canceled' });
    await expect(b).rejects.toMatchObject({ code: 'canceled' });
    expect(ch.pendingCount).toBe(0);
  });
});
