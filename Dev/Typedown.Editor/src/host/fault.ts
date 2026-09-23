import type { LifecycleFaultPayload } from '../bridge/protocol';

/**
 * lifecycle.fault（docs/editor-protocol.md 第 3 节）：window.onerror、unhandledrejection 与 CM6 的 exceptionSink 都接到这里。
 * 同一 message + stack 在一次页面生命周期内只报一次。fatal 由调用方判定（启动失败、doc.load 失败为 fatal，其余只记日志）。
 */
export class FaultReporter {
  private readonly seen = new Set<string>();

  constructor(private readonly emit: (p: LifecycleFaultPayload) => void) {}

  report(error: unknown, fatal: boolean) {
    const { message, stack } = describe(error);
    const key = `${message}\n${stack ?? ''}`;
    if (this.seen.has(key)) return;
    this.seen.add(key);
    try {
      this.emit({ message, stack: stack ?? null, fatal });
    } catch (e) {
      // 上报本身失败（通道已断）时只能留在控制台
      console.error('[bridge] lifecycle.fault 发送失败', e);
    }
  }

  /** 挂 window 级的错误监听；isFatal 在出错时求值。返回卸载函数 */
  install(win: Window, isFatal: () => boolean): () => void {
    const onError = (e: ErrorEvent) => this.report(e.error ?? e.message, isFatal());
    const onRejection = (e: PromiseRejectionEvent) => this.report(e.reason, isFatal());
    win.addEventListener('error', onError);
    win.addEventListener('unhandledrejection', onRejection);
    return () => {
      win.removeEventListener('error', onError);
      win.removeEventListener('unhandledrejection', onRejection);
    };
  }
}

export function describe(error: unknown): { message: string; stack?: string } {
  if (error instanceof Error) return { message: error.message || error.name, stack: error.stack };
  if (typeof error === 'string') return { message: error };
  try { return { message: JSON.stringify(error) ?? String(error) }; } catch { return { message: String(error) }; }
}
