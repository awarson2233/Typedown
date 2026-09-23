import type { ViewViewportPayload } from '../bridge/protocol';

/**
 * 视口上报与宿主滚动（docs/editor-protocol.md 第 8 节；做法照搬旧页面 services/scrollbar.ts）。
 * XAML 滚动条是唯一的滚动条：CM6 不设固定高度、以窗口为滚动容器，页面隐藏原生滚动条（styles/editor.css 的 .td-host）。
 * 尺寸、滚动、宿主请求可能在同一帧里接连触发，合并到下一帧读布局稳定后的尺寸；与上次相同则不发，宿主 refreshViewport 后必发。
 */

/**
 * 分数缩放（如 175%）下 CSS 视口宽度是小数，innerWidth 与 scrollWidth 各自取整后可能差出 1 px，
 * 宿主就会画出一条滚不动的滚动条。不超过 1 px 的「溢出」一律视为没有。
 */
const EPSILON = 1;
export const overflow = (content: number, viewport: number) => {
  const delta = content - viewport;
  return delta <= EPSILON ? 0 : delta;
};

export function measureViewport(win: Window): ViewViewportPayload {
  const root = win.document.documentElement, body = win.document.body;
  return {
    viewportWidth: win.innerWidth,
    viewportHeight: win.innerHeight,
    maximumX: overflow(Math.max(root.scrollWidth, body?.scrollWidth ?? 0), win.innerWidth),
    maximumY: overflow(Math.max(root.scrollHeight, body?.scrollHeight ?? 0), win.innerHeight),
    scrollX: win.scrollX,
    scrollY: win.scrollY,
  };
}

export class ViewportReporter {
  private last = '';
  private force = false;

  constructor(
    private readonly win: Window,
    private readonly emit: (p: ViewViewportPayload) => void,
    /** 排进 FrameQueue（其他槽位） */
    private readonly schedule: (task: () => void) => void,
  ) {}

  private readonly task = () => {
    const p = measureViewport(this.win);
    const key = `${p.viewportWidth},${p.viewportHeight},${p.maximumX},${p.maximumY},${p.scrollX},${p.scrollY}`;
    if (!this.force && key === this.last) return;
    this.force = false;
    this.last = key;
    this.emit(p);
  };

  /** 下一帧上报；force 为真时即使与上次相同也发（view.refreshViewport） */
  request(force = false) {
    if (force) this.force = true;
    this.schedule(this.task);
  }

  /** 挂上尺寸与滚动监听，返回卸载函数 */
  install(): () => void {
    const onChange = () => this.request();
    const ro = new ResizeObserver(onChange);
    // body 带 min-width 时视口窄于它之后 body 不再变化，只有根元素还跟着视口变；正文长高时 body 变
    ro.observe(this.win.document.documentElement);
    if (this.win.document.body) ro.observe(this.win.document.body);
    this.win.addEventListener('scroll', onChange, { passive: true });
    this.win.addEventListener('resize', onChange);
    return () => {
      ro.disconnect();
      this.win.removeEventListener('scroll', onChange);
      this.win.removeEventListener('resize', onChange);
    };
  }

  /** view.scrollTo：差不到 1 px 不动，避免与宿主来回校正；引起的滚动照常触发 view.viewport */
  scrollTo(x: number, y: number) {
    const near = (a: number, b: number) => Math.abs(a - b) < 1;
    if (!near(x, this.win.scrollX) || !near(y, this.win.scrollY)) this.win.scrollTo(x, y);
  }
}
