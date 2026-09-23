import { describe, expect, it } from 'vitest';
import { FrameQueue, Slot } from '../src/bridge/frame';

function manual() {
  const frames: (() => void)[] = [];
  const q = new FrameQueue(run => frames.push(run));
  return { q, frames, next: () => frames.shift()!() };
}

describe('同帧事件顺序', () => {
  it('无论登记先后，一帧内按 doc → history → selection → marks → 其他 执行', () => {
    const { q, frames, next } = manual();
    const log: string[] = [];
    q.schedule(Slot.Other, () => log.push('outline'));
    q.schedule(Slot.Marks, () => log.push('marks'));
    q.schedule(Slot.Selection, () => log.push('selection'));
    q.schedule(Slot.History, () => log.push('history'));
    q.schedule(Slot.Doc, () => log.push('doc'));
    expect(frames.length).toBe(1);
    next();
    expect(log).toEqual(['doc', 'history', 'selection', 'marks', 'outline']);
  });

  it('同一任务一帧只跑一次', () => {
    const { q, next } = manual();
    let n = 0;
    const t = () => n++;
    q.schedule(Slot.Selection, t);
    q.schedule(Slot.Selection, t);
    next();
    expect(n).toBe(1);
  });

  it('任务里登记更靠后的槽位：本帧接着跑；登记同槽或更靠前的：留到下一帧', () => {
    const { q, frames, next } = manual();
    const log: string[] = [];
    q.schedule(Slot.Selection, () => {
      log.push('selection');
      q.schedule(Slot.Other, () => log.push('other'));
      q.schedule(Slot.Doc, () => log.push('doc'));
    });
    next();
    expect(log).toEqual(['selection', 'other']);
    expect(frames.length).toBe(1);
    next();
    expect(log).toEqual(['selection', 'other', 'doc']);
  });

  it('一个任务抛异常不影响同帧的其他任务', () => {
    const frames: (() => void)[] = [];
    const errors: unknown[] = [];
    const q = new FrameQueue(run => frames.push(run), e => errors.push(e));
    const log: string[] = [];
    q.schedule(Slot.Doc, () => { throw new Error('x'); });
    q.schedule(Slot.Doc, () => log.push('doc2'));
    q.schedule(Slot.Other, () => log.push('other'));
    frames.shift()!();
    expect(log).toEqual(['doc2', 'other']);
    expect(errors.length).toBe(1);
  });
});
