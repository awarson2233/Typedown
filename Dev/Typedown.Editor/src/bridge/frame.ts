/**
 * 按动画帧合并的事件出口（docs/editor-protocol.md 第 6 节）。
 * 同一帧里的事件顺序固定为 doc.changed → history.changed → selection.changed → selection.marks → 其他：
 * 宿主处理 selection.changed 时镜像已经是同一帧的正文，偏移不会错位。
 * 每个任务按函数身份去重，一帧最多跑一次；任务里再排到更靠后的槽位的，本帧内接着跑，排到同槽或更靠前的留到下一帧。
 */

export const Slot = { Doc: 0, History: 1, Selection: 2, Marks: 3, Other: 4 } as const;
export type Slot = typeof Slot[keyof typeof Slot];

export class FrameQueue {
  private readonly slots: Set<() => void>[] = Object.values(Slot).map(() => new Set());
  private requested = false;
  private running = -1;

  /** 默认 requestAnimationFrame；单测里注入手动触发的调度器 */
  constructor(
    private readonly request: (run: () => void) => void = run => requestAnimationFrame(() => run()),
    // 一个任务出错不能拖垮同帧的其他事件；默认把异常改抛到下一个微任务，照常进 window.onerror → lifecycle.fault
    private readonly onError: (e: unknown) => void = e => queueMicrotask(() => { throw e; }),
  ) {}

  schedule(slot: Slot, task: () => void) {
    this.slots[slot].add(task);
    // 本帧还没轮到的槽位不用另请一帧
    if (this.running >= 0 && slot > this.running) return;
    if (!this.requested) {
      this.requested = true;
      this.request(() => this.run());
    }
  }

  /** 立即跑完所有挂起的任务（按槽位顺序） */
  run() {
    this.requested = false;
    for (let s = 0; s < this.slots.length; s++) {
      const set = this.slots[s];
      if (!set.size) continue;
      const tasks = [...set];
      set.clear();
      this.running = s;
      for (const t of tasks) {
        try { t(); } catch (e) { this.onError(e); }
      }
    }
    this.running = -1;
  }
}
