import {
  WIRE_ERRORS,
  type CommandMap, type CommandType, type EventMap, type EventType, type HostRequestMap, type HostRequestType, type HostToPage,
  type PageRequestMap, type PageRequestType, type PageToHost, type WireError,
} from './protocol';

/**
 * 传输与信封（docs/editor-protocol.md 第 2、9 节）。
 * - 页面→宿主：chrome.webview.postMessage(JSON 字符串)；宿主→页面：chrome.webview 的 message 事件，data 是 JSON 字符串。
 * - cmd / evt 即发即走；req / res 两个方向各有自己的 id 计数与挂起表，一端只用对方的 res 了结自己发出的 req。
 * - 不认识的 cmd 只记日志；不认识的 req 回 unknownType；ready 之前收到的 req 回 notReady，cmd 排队到 open() 后按序处理。
 * - 没有 chrome.webview 时（dev.html、普通浏览器、单测）用 FakeHost 代替宿主。
 */

/** 双向字符串通道：WebView2 或假宿主 */
export interface Transport {
  send(data: string): void;
  /** 只注册一次；data 通常是字符串，宿主误用 PostWebMessageAsJson 时是对象，两种都收 */
  listen(onMessage: (data: unknown) => void): void;
}

interface WebView2 {
  postMessage(data: string): void;
  addEventListener(type: 'message', listener: (e: { data: unknown }) => void): void;
}

/** 真实宿主的传输；不在 WebView2 里时返回 null */
export function webviewTransport(): Transport | null {
  const webview = (globalThis as { chrome?: { webview?: WebView2 } }).chrome?.webview;
  if (!webview) return null;
  return {
    send: data => webview.postMessage(data),
    listen: onMessage => webview.addEventListener('message', e => onMessage(e.data)),
  };
}

export class ChannelError extends Error {
  constructor(readonly code: WireError | 'timeout', message?: string) {
    super(message ?? code);
    this.name = 'ChannelError';
  }
}

/** 必填字段的类型（只列本页面实际处理的消息；缺失、为 null 或类型不对按 invalidPayload） */
type FieldType = 'number' | 'string' | 'boolean' | 'object' | 'array';
const REQUIRED: Partial<Record<CommandType | HostRequestType, Record<string, FieldType>>> = {
  'doc.load': { version: 'number', text: 'string', basePath: 'string' },
  'outline.reveal': { id: 'string' },
  'view.settings': { changes: 'object' },
  'view.theme': { theme: 'object' },
  'view.keymap': { chords: 'array' },
  'view.scrollTo': { x: 'number', y: 'number' },
};

const typeOf = (v: unknown): FieldType | 'null' | 'other' =>
  v === null ? 'null' : Array.isArray(v) ? 'array' : (['number', 'string', 'boolean', 'object'] as const).find(t => typeof v === t) ?? 'other';

/** 载荷校验：返回第一个不合格的字段名，合格时返回 null */
export function invalidField(t: string, p: Record<string, unknown>): string | null {
  const req = REQUIRED[t as CommandType];
  if (!req) return null;
  for (const [name, type] of Object.entries(req)) if (typeOf(p[name]) !== type) return name;
  return null;
}

type Decoded =
  | { k: 'cmd'; t: string; p: Record<string, unknown> }
  | { k: 'req'; id: number; t: string; p: Record<string, unknown> }
  | { k: 'res'; id: number; ok: true; p: unknown }
  | { k: 'res'; id: number; ok: false; err: { code: WireError; message?: string } };

const isId = (v: unknown): v is number => typeof v === 'number' && Number.isSafeInteger(v) && v >= 0;
const isObject = (v: unknown): v is Record<string, unknown> => typeOf(v) === 'object';

/** 解析宿主来的一条报文；不合格时给出原因，能取到请求 id 的另带 reqId（据此回 invalidPayload） */
export function decodeEnvelope(data: unknown): Decoded | { error: string; reqId?: number } {
  let m: unknown = data;
  if (typeof data === 'string') {
    try { m = JSON.parse(data); } catch { return { error: 'JSON 解析失败' }; }
  }
  if (!isObject(m)) return { error: '报文不是对象' };
  const { k, t, id } = m;
  // 载荷永远是对象；缺省按 {} 处理
  const p = m.p === undefined ? {} : m.p;
  switch (k) {
    case 'cmd':
      if (typeof t !== 'string') return { error: 'cmd 缺少 t' };
      if (!isObject(p)) return { error: `${t} 的载荷不是对象` };
      return { k, t, p };
    case 'req':
      if (!isId(id)) return { error: 'req 缺少合法的 id' };
      if (typeof t !== 'string') return { error: 'req 缺少 t', reqId: id };
      if (!isObject(p)) return { error: `${t} 的载荷不是对象`, reqId: id };
      return { k, id, t, p };
    case 'res':
      if (!isId(id)) return { error: 'res 缺少合法的 id' };
      // 结果可空的请求应答可以是 "p":null；缺 p 按 {} 读
      if (m.ok === true) return { k, id, ok: true, p: m.p === undefined ? {} : m.p };
      if (m.ok === false) {
        const err = isObject(m.err) ? m.err : {};
        const code = WIRE_ERRORS.includes(err.code as WireError) ? err.code as WireError : 'failed';
        return { k, id, ok: false, err: { code, message: typeof err.message === 'string' ? err.message : undefined } };
      }
      return { error: 'res 缺少 ok' };
    default:
      return { error: `未知的报文种类 ${String(k)}` };
  }
}

export interface ChannelOptions {
  /** 诊断日志（默认 console.warn） */
  log?: (message: string) => void;
  /** 命令或请求的处理函数抛出异常时回调（接到 lifecycle.fault） */
  onHandlerError?: (type: string, error: unknown) => void;
}

interface Pending {
  resolve(value: unknown): void;
  reject(error: ChannelError): void;
  timer?: ReturnType<typeof setTimeout>;
}

export class Channel {
  private readonly commands = new Map<string, (p: never) => void>();
  private readonly requests = new Map<string, (p: never) => unknown>();
  /** 页面发出、等待宿主应答的请求 */
  private readonly pending = new Map<number, Pending>();
  private nextId = 1;
  private opened = false;
  private early: { t: string; p: Record<string, unknown> }[] = [];
  private readonly log: (message: string) => void;

  constructor(private readonly transport: Transport, private readonly opts: ChannelOptions = {}) {
    this.log = opts.log ?? (message => console.warn(`[bridge] ${message}`));
    transport.listen(data => this.receive(data));
  }

  onCommand<T extends CommandType>(t: T, handler: (p: CommandMap[T]) => void): this {
    this.commands.set(t, handler as (p: never) => void);
    return this;
  }

  onRequest<T extends HostRequestType>(t: T, handler: (p: HostRequestMap[T][0]) => HostRequestMap[T][1] | Promise<HostRequestMap[T][1]>): this {
    this.requests.set(t, handler as (p: never) => unknown);
    return this;
  }

  get isOpen() { return this.opened; }

  /** 处理函数都已注册、可以接收 doc.load：重放排队的命令。之后由调用方发 lifecycle.ready */
  open() {
    if (this.opened) return;
    this.opened = true;
    const early = this.early;
    this.early = [];
    for (const c of early) this.runCommand(c.t, c.p);
  }

  emit<T extends EventType>(t: T, p: EventMap[T]) {
    this.post({ k: 'evt', t, p } as PageToHost);
  }

  /** 页面→宿主的请求。协议不设超时（可能在等用户操作对话框）；timeoutMs 只给调用方自己兜底 */
  request<T extends PageRequestType>(t: T, p: PageRequestMap[T][0], timeoutMs?: number): Promise<PageRequestMap[T][1]> {
    const id = this.nextId++;
    return new Promise((resolve, reject) => {
      const entry: Pending = { resolve: resolve as (v: unknown) => void, reject };
      if (timeoutMs !== undefined) {
        entry.timer = setTimeout(() => {
          this.pending.delete(id);
          reject(new ChannelError('timeout', `${t} 在 ${timeoutMs} ms 内没有应答`));
        }, timeoutMs);
      }
      this.pending.set(id, entry);
      this.post({ k: 'req', id, t, p } as PageToHost);
    });
  }

  get pendingCount() { return this.pending.size; }

  /** 页面卸载等场合：挂起的请求一律以 canceled 结束 */
  cancelAll(message = '通道关闭') {
    const all = [...this.pending.values()];
    this.pending.clear();
    for (const e of all) { clearTimeout(e.timer); e.reject(new ChannelError('canceled', message)); }
  }

  post(msg: PageToHost) {
    this.transport.send(JSON.stringify(msg));
  }

  receive(data: unknown) {
    const m = decodeEnvelope(data);
    if ('error' in m) {
      this.log(`丢弃报文：${m.error}`);
      // 请求至少拿到了 id 时要应答，否则宿主那边会一直挂到超时
      if (m.reqId !== undefined) this.post({ k: 'res', id: m.reqId, ok: false, err: { code: 'invalidPayload', message: m.error } } as PageToHost);
      return;
    }
    switch (m.k) {
      case 'cmd':
        if (!this.opened) { this.early.push({ t: m.t, p: m.p }); return; }
        this.runCommand(m.t, m.p);
        return;
      case 'req':
        void this.runRequest(m.id, m.t, m.p);
        return;
      case 'res': {
        const e = this.pending.get(m.id);
        if (!e) { this.log(`没有挂起的请求 ${m.id}（已超时或重复应答）`); return; }
        this.pending.delete(m.id);
        clearTimeout(e.timer);
        if (m.ok) e.resolve(m.p);
        else e.reject(new ChannelError(m.err.code, m.err.message));
        return;
      }
    }
  }

  private runCommand(t: string, p: Record<string, unknown>) {
    const handler = this.commands.get(t);
    if (!handler) { this.log(`未处理的命令 ${t}`); return; }
    const bad = invalidField(t, p);
    if (bad) { this.log(`命令 ${t} 的载荷不合格：${bad}`); return; }
    try {
      handler(p as never);
    } catch (e) {
      this.log(`命令 ${t} 处理失败：${String(e)}`);
      this.opts.onHandlerError?.(t, e);
    }
  }

  private async runRequest(id: number, t: string, p: Record<string, unknown>) {
    const fail = (code: WireError, message?: string) => this.post({ k: 'res', id, ok: false, err: message === undefined ? { code } : { code, message } } as PageToHost);
    if (!this.opened) { fail('notReady'); return; }
    const handler = this.requests.get(t);
    if (!handler) { fail('unknownType', t); return; }
    const bad = invalidField(t, p);
    if (bad) { fail('invalidPayload', bad); return; }
    let result: unknown;
    try {
      result = await handler(p as never);
    } catch (e) {
      if (e instanceof ChannelError && e.code !== 'timeout') { fail(e.code, e.message); return; }
      this.opts.onHandlerError?.(t, e);
      fail('failed', e instanceof Error ? e.message : String(e));
      return;
    }
    this.post({ k: 'res', id, ok: true, p: result === undefined ? {} : result } as PageToHost);
  }
}


/**
 * 假宿主：没有 chrome.webview 时代替 WebView2，也是单测驱动握手的工具。
 * 页面侧把它当 Transport 用；宿主侧用 command / request / respond 与页面对话，received 记下页面发出的全部报文。
 * 默认同步投递（单测好断言）；async: true 时像 WebView2 一样放到下一个任务。
 */
export class FakeHost implements Transport {
  readonly received: PageToHost[] = [];
  private toPage: ((data: unknown) => void) | null = null;
  private readonly listeners = new Set<(msg: PageToHost) => void>();
  private readonly pending = new Map<number, { resolve(v: unknown): void; reject(e: ChannelError): void }>();
  private readonly handlers = new Map<string, (p: never) => unknown>();
  private nextId = 1;

  constructor(private readonly options: { async?: boolean } = {}) {}

  // ── Transport（页面一侧） ──
  send(data: string) {
    const msg = JSON.parse(data) as PageToHost;
    this.received.push(msg);
    if (msg.k === 'res') {
      const e = this.pending.get(msg.id);
      if (e) {
        this.pending.delete(msg.id);
        if (msg.ok) e.resolve(msg.p);
        else e.reject(new ChannelError(msg.err.code, msg.err.message));
      }
    } else if (msg.k === 'req') {
      this.answer(msg.id, msg.t, msg.p);
    }
    for (const l of this.listeners) l(msg);
  }

  listen(onMessage: (data: unknown) => void) { this.toPage = onMessage; }

  // ── 宿主一侧 ──
  /** 订阅页面发出的报文，返回退订函数 */
  onMessage(listener: (msg: PageToHost) => void): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  /** 页面发来的某类事件的载荷（按到达顺序） */
  events<T extends EventType>(t: T): EventMap[T][] {
    return this.received.filter(m => m.k === 'evt' && m.t === t).map(m => (m as unknown as { p: EventMap[T] }).p);
  }

  /** 发一条原始报文（字符串原样投递，单测构造畸形报文用） */
  post(msg: HostToPage | string) {
    const data = typeof msg === 'string' ? msg : JSON.stringify(msg);
    if (this.options.async) setTimeout(() => this.toPage?.(data), 0);
    else this.toPage?.(data);
  }

  command<T extends CommandType>(t: T, p: CommandMap[T]) {
    this.post({ k: 'cmd', t, p } as HostToPage);
  }

  request<T extends HostRequestType>(t: T, p: HostRequestMap[T][0]): Promise<HostRequestMap[T][1]> {
    const id = this.nextId++;
    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve: resolve as (v: unknown) => void, reject });
      this.post({ k: 'req', id, t, p } as HostToPage);
    });
  }

  /** 登记页面→宿主请求的应答；没登记的类型回 unknownType（页面走降级路径） */
  respond<T extends PageRequestType>(t: T, handler: (p: PageRequestMap[T][0]) => PageRequestMap[T][1] | Promise<PageRequestMap[T][1]>) {
    this.handlers.set(t, handler as (p: never) => unknown);
  }

  private async answer(id: number, t: string, p: unknown) {
    const h = this.handlers.get(t);
    const reply = (msg: HostToPage) => this.post(msg);
    if (!h) { reply({ k: 'res', id, ok: false, err: { code: 'unknownType', message: t } }); return; }
    try {
      reply({ k: 'res', id, ok: true, p: (await h(p as never)) as never });
    } catch (e) {
      reply({ k: 'res', id, ok: false, err: { code: 'failed', message: String(e) } });
    }
  }
}
