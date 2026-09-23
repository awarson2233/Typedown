// 无头 Edge + CDP 的最小驱动：每次调用起一个独立的浏览器进程与临时 user-data-dir，用完关掉。
import { spawn } from 'node:child_process';
import { mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

export const sleep = ms => new Promise(r => setTimeout(r, ms));
const EDGE = process.env.EDGE ?? 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';

/**
 * @param {number} port 远程调试端口（约定从 9360 起）
 * @param {string[]} flags 额外的浏览器参数（例如与宿主 Config.WebView2Args 相同的一组）
 */
export async function launch(port, flags = []) {
  const udd = mkdtempSync(join(tmpdir(), 'td-probe-'));
  const proc = spawn(EDGE, ['--headless=new', `--remote-debugging-port=${port}`, `--user-data-dir=${udd}`,
    '--no-first-run', '--window-size=1200,900', ...flags, 'about:blank'], { stdio: 'ignore' });
  let wsUrl;
  for (let i = 0; i < 100 && !wsUrl; i++) {
    try { wsUrl = (await (await fetch(`http://127.0.0.1:${port}/json/list`)).json()).find(t => t.type === 'page')?.webSocketDebuggerUrl; } catch { }
    if (!wsUrl) await sleep(200);
  }
  if (!wsUrl) { proc.kill(); throw new Error('Edge 未启动'); }
  const ws = new WebSocket(wsUrl);
  await new Promise(r => ws.addEventListener('open', r));
  let seq = 0;
  const pending = new Map();
  const listeners = [];
  ws.addEventListener('message', e => {
    const m = JSON.parse(e.data);
    if (m.id && pending.has(m.id)) { pending.get(m.id)(m); pending.delete(m.id); }
    else if (m.method) for (const l of listeners) l(m);
  });
  const send = (method, params = {}, ms = 60000) => new Promise(r => {
    const id = ++seq;
    pending.set(id, r);
    ws.send(JSON.stringify({ id, method, params }));
    setTimeout(() => { if (pending.has(id)) { pending.delete(id); r({ timeout: true }); } }, ms);
  });
  const evalJs = async (expr, ms = 60000) => {
    const r = await send('Runtime.evaluate', { expression: expr, returnByValue: true, awaitPromise: true }, ms);
    if (r.timeout) return { __timeout: true };
    if (r.result?.exceptionDetails) return { __error: r.result.exceptionDetails.exception?.description ?? r.result.exceptionDetails.text };
    return r.result?.result?.value;
  };
  const close = async () => {
    try { ws.close(); } catch { }
    proc.kill();
    await sleep(1000);
    try { rmSync(udd, { recursive: true, force: true }); } catch { }
  };
  return { send, evalJs, close, on: f => listeners.push(f) };
}
